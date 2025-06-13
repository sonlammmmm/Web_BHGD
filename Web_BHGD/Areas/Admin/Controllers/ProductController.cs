using Web_BHGD.Models;
using Web_BHGD.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Authorization;
using Web_BHGD.Areas.Admin.Models;

namespace Web_BHGD.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ILogger<ProductController> _logger;

        // Đường dẫn ảnh mặc định
        private const string DefaultImagePath = "/images/default_product.png";

        public ProductController(IProductRepository productRepository,
       ICategoryRepository categoryRepository,
       ILogger<ProductController> logger)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _logger = logger;
        }

        // Hiển thị danh sách sản phẩm
        public async Task<IActionResult> Index()
        {
            var products = await _productRepository.GetAllAsync();
            return View(products);
        }

        // Hiển thị form thêm sản phẩm mới
        public async Task<IActionResult> Add()
        {
            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View();
        }

        // Xử lý thêm sản phẩm mới
        [HttpPost]
        public async Task<IActionResult> Add(Product product, IFormFile imageUrl)
        {
            ModelState.Remove("ImageUrl");
            if (ModelState.IsValid)
            {
                try
                {
                    _logger.LogInformation("Bắt đầu thêm sản phẩm: {ProductName}", product.Name);
                    if (imageUrl != null && imageUrl.Length > 0)
                    {
                        product.ImageUrl = await SaveImage(imageUrl);
                    }
                    else
                    {
                        product.ImageUrl = DefaultImagePath;
                    }

                    await _productRepository.AddAsync(product);
                    _logger.LogInformation("Thêm sản phẩm thành công: {ProductName}", product.Name);
                    return RedirectToAction(nameof(Index));
                }
                catch (ArgumentException ex)
                {
                    _logger.LogError(ex, "Lỗi tham số: {ProductName}", product.Name);
                    ModelState.AddModelError("imageUrl", ex.Message);
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogError(ex, "Lỗi vận hành: {ProductName}", product.Name);
                    ModelState.AddModelError("", $"Lỗi khi lưu sản phẩm: {ex.Message}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi không xác định: {ProductName}", product.Name);
                    ModelState.AddModelError("", $"Lỗi không xác định: {ex.Message}");
                }
            }

            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View(product);
        }

        private async Task<string> SaveImage(IFormFile image)
        {
            try
            {
                if (image == null || image.Length == 0)
                {
                    throw new ArgumentException("File ảnh không hợp lệ.");
                }

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                {
                    throw new ArgumentException("Chỉ chấp nhận file ảnh (.jpg, .jpeg, .png, .gif).");
                }

                var fileName = Guid.NewGuid().ToString() + extension;
                var savePath = Path.Combine("wwwroot/images", fileName);
                var directory = Path.GetDirectoryName(savePath);

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using (var fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await image.CopyToAsync(fileStream);
                }

                if (!System.IO.File.Exists(savePath))
                {
                    throw new InvalidOperationException("Không thể lưu file ảnh.");
                }

                _logger.LogInformation("Lưu ảnh thành công: {FilePath}", savePath);
                return "/images/" + fileName;
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Lỗi I/O khi lưu ảnh: {FileName}", image.FileName);
                throw new InvalidOperationException("Lỗi khi lưu file ảnh.", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi không xác định khi lưu ảnh: {FileName}", image.FileName);
                throw new InvalidOperationException($"Lỗi không xác định: {ex.Message}", ex);
            }
        }

        public async Task<IActionResult> Display(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        // Hiển thị form cập nhật sản phẩm
        public async Task<IActionResult> Update(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name",
           product.CategoryId);
            return View(product);
        }

        // Xử lý cập nhật sản phẩm
        [HttpPost]
        public async Task<IActionResult> Update(int id, Product product, IFormFile imageUrl)
        {
            ModelState.Remove("ImageUrl");
            if (id != product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _logger.LogInformation("Bắt đầu cập nhật sản phẩm: {ProductId}", id);
                    var existingProduct = await _productRepository.GetByIdAsync(id);
                    if (existingProduct == null)
                    {
                        return NotFound();
                    }

                    if (imageUrl != null && imageUrl.Length > 0)
                    {
                        if (!string.IsNullOrEmpty(existingProduct.ImageUrl) && existingProduct.ImageUrl != DefaultImagePath)
                        {
                            var oldImagePath = Path.Combine("wwwroot", existingProduct.ImageUrl.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                try
                                {
                                    System.IO.File.Delete(oldImagePath);
                                }
                                catch (IOException ex)
                                {
                                    _logger.LogError(ex, "Lỗi xóa ảnh cũ: {ImagePath}", oldImagePath);
                                    ModelState.AddModelError("", $"Lỗi xóa ảnh cũ: {ex.Message}");
                                    return View(product);
                                }
                            }
                        }
                        existingProduct.ImageUrl = await SaveImage(imageUrl);
                    }

                    existingProduct.Name = product.Name;
                    existingProduct.Price = product.Price;
                    existingProduct.Description = product.Description;
                    existingProduct.CategoryId = product.CategoryId;

                    await _productRepository.UpdateAsync(existingProduct);
                    _logger.LogInformation("Cập nhật sản phẩm thành công: {ProductId}", id);
                    return RedirectToAction(nameof(Index));
                }
                catch (ArgumentException ex)
                {
                    _logger.LogError(ex, "Lỗi tham số: {ProductId}", id);
                    ModelState.AddModelError("imageUrl", ex.Message);
                }
                catch (InvalidOperationException ex)
                {
                    _logger.LogError(ex, "Lỗi vận hành: {ProductId}", id);
                    ModelState.AddModelError("", $"Lỗi cập nhật sản phẩm: {ex.Message}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi không xác định: {ProductId}", id);
                    ModelState.AddModelError("", $"Lỗi không xác định: {ex.Message}");
                }
            }

            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        // Hiển thị form xác nhận xóa sản phẩm
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        // Xử lý xóa sản phẩm
        [HttpPost, ActionName("DeleteConfirmed")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _productRepository.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}