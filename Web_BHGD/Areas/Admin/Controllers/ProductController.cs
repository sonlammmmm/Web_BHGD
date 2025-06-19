using Web_BHGD.Models;
using Web_BHGD.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.IO;
using Web_BHGD.Areas.Admin.Models;

namespace Web_BHGD.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProductController> _logger;
        private readonly string _imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");
        private const string DefaultImagePath = "/images/default_product.png";
        private const long MaxFileSize = 5 * 1024 * 1024; // 5MB

        public ProductController(
            IProductRepository productRepository,
            ICategoryRepository categoryRepository,
            ApplicationDbContext context,
            ILogger<ProductController> logger)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _productRepository.GetAllAsync();

            // Thống kê chi tiết hơn
            ViewBag.TotalProducts = products.Count();
            ViewBag.LowStockCount = products.Count(p => p.Stock > 0 && p.Stock <= 10);
            ViewBag.OutOfStockCount = products.Count(p => p.Stock == 0);
            ViewBag.HighStockCount = products.Count(p => p.Stock > 50);
            ViewBag.TotalValue = products.Sum(p => p.Stock * p.Price);
            ViewBag.TotalSold = products.Sum(p => p.SoldQuantity);

            return View(products);
        }

        public async Task<IActionResult> Display(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            // Tính toán các thông số chi tiết
            ViewBag.StockStatus = GetStockStatus(product.Stock);
            ViewBag.StockStatusClass = GetStockStatusClass(product.Stock);

            // Tính tỷ lệ bán hàng (dựa trên tổng sản lượng ban đầu)
            var totalInitialQuantity = product.Stock + product.SoldQuantity;
            ViewBag.SalesPercentage = totalInitialQuantity > 0
                ? Math.Round((double)product.SoldQuantity / totalInitialQuantity * 100, 1)
                : 0;

            // Tính giá trị tồn kho
            ViewBag.StockValue = product.Stock * product.Price;

            // Tính doanh thu đã bán
            ViewBag.Revenue = product.SoldQuantity * product.Price;

            // Đánh giá hiệu suất bán hàng
            ViewBag.PerformanceStatus = GetPerformanceStatus(product.SoldQuantity, product.Stock);

            return View(product);
        }

        public async Task<IActionResult> Add()
        {
            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Add(Product product, IFormFile imageUrl)
        {
            _logger.LogInformation("Bắt đầu thêm sản phẩm: {ProductName}, CategoryId: {CategoryId}, Stock: {Stock}",
                product.Name, product.CategoryId, product.Stock);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState không hợp lệ: {Errors}",
                    string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                var categories = await _categoryRepository.GetAllAsync();
                ViewBag.Categories = new SelectList(categories, "Id", "Name");
                return View(product);
            }

            if (product.Stock < 0)
            {
                _logger.LogWarning("Số lượng tồn kho không hợp lệ: {Stock}", product.Stock);
                ModelState.AddModelError("Stock", "Số lượng tồn kho phải lớn hơn hoặc bằng 0");
                var categories = await _categoryRepository.GetAllAsync();
                ViewBag.Categories = new SelectList(categories, "Id", "Name");
                return View(product);
            }

            try
            {
                // Khởi tạo sản phẩm mới
                product.SoldQuantity = 0; // Sản phẩm mới chưa bán được gì
                product.ImageUrl = imageUrl != null && imageUrl.Length > 0
                    ? await SaveImage(imageUrl)
                    : DefaultImagePath;

                await _productRepository.AddAsync(product);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Thêm sản phẩm thành công: {ProductName} - Tồn kho: {Stock}",
                    product.Name, product.Stock);

                TempData["Success"] = $"Thêm sản phẩm '{product.Name}' thành công với {product.Stock} đơn vị tồn kho";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Lỗi cơ sở dữ liệu khi thêm sản phẩm: {ProductName}", product.Name);
                ModelState.AddModelError("", $"Lỗi cơ sở dữ liệu: {ex.InnerException?.Message ?? ex.Message}");
                var categories = await _categoryRepository.GetAllAsync();
                ViewBag.Categories = new SelectList(categories, "Id", "Name");
                return View(product);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi không xác định khi thêm sản phẩm: {ProductName}", product.Name);
                ModelState.AddModelError("", $"Lỗi: {ex.Message}");
                var categories = await _categoryRepository.GetAllAsync();
                ViewBag.Categories = new SelectList(categories, "Id", "Name");
                return View(product);
            }
        }

        public async Task<IActionResult> Update(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);

            // Thông tin để hiển thị trên form
            ViewBag.CurrentStock = product.Stock;
            ViewBag.SoldQuantity = product.SoldQuantity;
            ViewBag.StockValue = product.Stock * product.Price;
            ViewBag.Revenue = product.SoldQuantity * product.Price;

            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> Update(int id, Product product, IFormFile imageUrl)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                var categories = await _categoryRepository.GetAllAsync();
                ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);
                ViewBag.CurrentStock = product.Stock;
                ViewBag.SoldQuantity = product.SoldQuantity;
                return View(product);
            }

            if (product.Stock < 0)
            {
                ModelState.AddModelError("Stock", "Số lượng tồn kho phải lớn hơn hoặc bằng 0");
                var categories = await _categoryRepository.GetAllAsync();
                ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);
                return View(product);
            }

            try
            {
                var existingProduct = await _productRepository.GetByIdAsync(id);
                if (existingProduct == null)
                {
                    return NotFound();
                }

                var oldStock = existingProduct.Stock;
                var oldImageUrl = existingProduct.ImageUrl;

                // Xử lý ảnh mới
                if (imageUrl != null && imageUrl.Length > 0)
                {
                    existingProduct.ImageUrl = await SaveImage(imageUrl);
                    if (!string.IsNullOrEmpty(oldImageUrl) && oldImageUrl != DefaultImagePath)
                    {
                        var oldImagePath = Path.Combine("wwwroot", oldImageUrl.TrimStart('/'));
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }
                }

                // Cập nhật thông tin sản phẩm (giữ nguyên SoldQuantity)
                existingProduct.Name = product.Name;
                existingProduct.Price = product.Price;
                existingProduct.Description = product.Description;
                existingProduct.CategoryId = product.CategoryId;
                existingProduct.Stock = product.Stock;
                // Không cập nhật SoldQuantity ở đây vì nó được quản lý bởi OrderController

                await _productRepository.UpdateAsync(existingProduct);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Cập nhật sản phẩm thành công: {ProductId} - Tồn kho từ {OldStock} thành {NewStock}",
                    id, oldStock, product.Stock);

                TempData["Success"] = $"Cập nhật sản phẩm '{product.Name}' thành công. Tồn kho hiện tại: {product.Stock}";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật sản phẩm: {ProductId}", id);
                ModelState.AddModelError("", $"Lỗi: {ex.Message}");
                var categories = await _categoryRepository.GetAllAsync();
                ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);
                return View(product);
            }
        }

        public async Task<IActionResult> Delete(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            // Kiểm tra xem sản phẩm có đang trong đơn hàng chưa xử lý không
            var pendingOrders = await _context.OrderDetails
                .Include(od => od.Order)
                .Where(od => od.ProductId == id &&
                            (od.Order.Status == "Chờ xác nhận" ||
                             od.Order.Status == "Đã xác nhận" ||
                             od.Order.Status == "Đang giao hàng"))
                .CountAsync();

            if (pendingOrders > 0)
            {
                ViewBag.DeleteWarning = $"Cảnh báo: Sản phẩm này đang có {pendingOrders} đơn hàng chưa hoàn thành!";
                ViewBag.CanDelete = false;
            }
            else
            {
                ViewBag.CanDelete = true;
                if (product.Stock > 0)
                {
                    ViewBag.StockWarning = $"Sản phẩm còn {product.Stock} đơn vị trong kho (giá trị: {(product.Stock * product.Price):N0} đ)";
                }
                if (product.SoldQuantity > 0)
                {
                    ViewBag.SalesInfo = $"Sản phẩm đã bán được {product.SoldQuantity} đơn vị (doanh thu: {(product.SoldQuantity * product.Price):N0} đ)";
                }
            }

            return View(product);
        }

        [HttpPost, ActionName("DeleteConfirmed")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            try
            {
                var product = await _productRepository.GetByIdAsync(id);
                if (product == null)
                {
                    return NotFound();
                }

                // Kiểm tra lại đơn hàng chưa xử lý
                var pendingOrders = await _context.OrderDetails
                    .Include(od => od.Order)
                    .Where(od => od.ProductId == id &&
                                (od.Order.Status == "Chờ xác nhận" ||
                                 od.Order.Status == "Đã xác nhận" ||
                                 od.Order.Status == "Đang giao hàng"))
                    .CountAsync();

                if (pendingOrders > 0)
                {
                    TempData["Error"] = $"Không thể xóa sản phẩm vì còn {pendingOrders} đơn hàng chưa hoàn thành";
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogInformation("Xóa sản phẩm: {ProductName} - Tồn kho: {Stock}, Đã bán: {SoldQuantity}",
                    product.Name, product.Stock, product.SoldQuantity);

                // Xóa ảnh
                if (!string.IsNullOrEmpty(product.ImageUrl) && product.ImageUrl != DefaultImagePath)
                {
                    var imagePath = Path.Combine("wwwroot", product.ImageUrl.TrimStart('/'));
                    if (System.IO.File.Exists(imagePath))
                    {
                        System.IO.File.Delete(imagePath);
                    }
                }

                await _productRepository.DeleteAsync(id);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"Xóa sản phẩm '{product.Name}' thành công";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa sản phẩm: {ProductId}", id);
                TempData["Error"] = $"Lỗi khi xóa sản phẩm: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        private async Task<string> SaveImage(IFormFile image)
        {
            // Giữ nguyên logic SaveImage như cũ
            _logger.LogInformation("Bắt đầu lưu ảnh: {FileName}, Kích thước: {FileSize} bytes", image.FileName, image.Length);

            if (image == null || image.Length == 0)
            {
                _logger.LogError("File ảnh không hợp lệ: null hoặc rỗng.");
                throw new ArgumentException("File ảnh không hợp lệ.");
            }

            if (image.Length > MaxFileSize)
            {
                _logger.LogError("File ảnh vượt quá kích thước tối đa: {FileSize} bytes.", image.Length);
                throw new ArgumentException("File ảnh vượt quá kích thước tối đa (5MB).");
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                _logger.LogError("Phần mở rộng không hợp lệ: {Extension}", extension);
                throw new ArgumentException("Chỉ chấp nhận file ảnh (.jpg, .jpeg, .png, .gif).");
            }

            try
            {
                using var stream = image.OpenReadStream();
                using var img = SixLabors.ImageSharp.Image.Load(stream);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "File không phải là ảnh hợp lệ: {FileName}", image.FileName);
                throw new ArgumentException("File không phải là ảnh hợp lệ.");
            }

            if (!Directory.Exists(_imagePath))
            {
                Directory.CreateDirectory(_imagePath);
            }

            var fileName = $"{Guid.NewGuid()}_{DateTime.UtcNow.Ticks}{extension}";
            var savePath = Path.Combine(_imagePath, fileName);

            try
            {
                using var stream = image.OpenReadStream();
                using var img = SixLabors.ImageSharp.Image.Load(stream);
                img.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new SixLabors.ImageSharp.Size(800, 800),
                    Mode = ResizeMode.Max
                }));
                await img.SaveAsync(savePath, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder { Quality = 75 });
                return "/images/" + fileName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lưu ảnh: {FileName}", image.FileName);
                throw new InvalidOperationException($"Lỗi khi lưu file ảnh: {ex.Message}", ex);
            }
        }

        // Các hàm helper mới
        private string GetStockStatus(int stock)
        {
            return stock switch
            {
                0 => "Hết hàng",
                <= 5 => "Sắp hết hàng",
                <= 10 => "Tồn kho thấp",
                <= 50 => "Tồn kho bình thường",
                _ => "Tồn kho cao"
            };
        }

        private string GetStockStatusClass(int stock)
        {
            return stock switch
            {
                0 => "stock-out",
                <= 5 => "stock-critical",
                <= 10 => "stock-low",
                <= 50 => "stock-normal",
                _ => "stock-high"
            };
        }

        private string GetPerformanceStatus(int soldQuantity, int stock)
        {
            var totalQuantity = soldQuantity + stock;
            if (totalQuantity == 0) return "Chưa có dữ liệu";

            var salesRate = (double)soldQuantity / totalQuantity;

            return salesRate switch
            {
                >= 0.8 => "Bán chạy",
                >= 0.5 => "Bán tốt",
                >= 0.2 => "Bán chậm",
                _ => "Bán rất chậm"
            };
        }
    }
}