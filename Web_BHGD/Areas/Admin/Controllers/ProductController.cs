using Web_BHGD.Models;
using Web_BHGD.Repositories;
using Web_BHGD.Areas.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Web_BHGD.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        public ProductController(IProductRepository productRepository,
        ICategoryRepository categoryRepository)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
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
            // Khai báo categories một lần ở đầu phương thức
            var categories = await _categoryRepository.GetAllAsync();

            try
            {
                if (ModelState.IsValid)
                {
                    // Kiểm tra danh mục tồn tại
                    var category = await _categoryRepository.GetByIdAsync(product.CategoryId);
                    if (category == null)
                    {
                        ModelState.AddModelError("CategoryId", "Danh mục không tồn tại");
                        ViewBag.Categories = new SelectList(categories, "Id", "Name");
                        return View(product);
                    }

                    if (imageUrl != null)
                    {
                        product.ImageUrl = await SaveImage(imageUrl);
                    }
                    await _productRepository.AddAsync(product);
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi khi thêm sản phẩm: " + ex.Message);
            }

            // Sử dụng lại categories đã khai báo
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View(product);
        }

        private async Task<string> SaveImage(IFormFile image)
        {
            if (image == null || image.Length == 0)
            {
                return null; // Không có hình ảnh, trả về null
            }

            // Kiểm tra định dạng file
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                throw new InvalidOperationException("Chỉ chấp nhận file hình ảnh (.jpg, .jpeg, .png, .gif)");
            }

            // Kiểm tra kích thước file (giới hạn 5MB)
            if (image.Length > 5 * 1024 * 1024)
            {
                throw new InvalidOperationException("Hình ảnh quá lớn, tối đa 5MB");
            }

            // Tạo tên file duy nhất để tránh trùng lặp
            var fileName = $"{Guid.NewGuid()}{extension}";
            var savePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images", fileName);

            // Đảm bảo thư mục tồn tại
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(savePath));
                using (var fileStream = new FileStream(savePath, FileMode.Create))
                {
                    await image.CopyToAsync(fileStream);
                }
                return "/images/" + fileName;
            }
            catch (IOException ex)
            {
                throw new InvalidOperationException("Lỗi khi lưu hình ảnh: " + ex.Message, ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new InvalidOperationException("Không có quyền ghi vào thư mục images: " + ex.Message, ex);
            }
        }

        // Hiển thị thông tin chi tiết sản phẩm
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
        [HttpPost]
        public async Task<IActionResult> Update(int id, Product product, IFormFile imageUrl)
        {
            ModelState.Remove("ImageUrl"); // Loại bỏ xác thực cho ImageUrl
            if (id != product.Id)
            {
                return NotFound();
            }

            // Khai báo categories một lần ở đầu phương thức
            var categories = await _categoryRepository.GetAllAsync();

            try
            {
                if (ModelState.IsValid)
                {
                    var existingProduct = await _productRepository.GetByIdAsync(id);
                    if (existingProduct == null)
                    {
                        return NotFound();
                    }

                    // Kiểm tra danh mục tồn tại
                    var category = await _categoryRepository.GetByIdAsync(product.CategoryId);
                    if (category == null)
                    {
                        ModelState.AddModelError("CategoryId", "Danh mục không tồn tại");
                        ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);
                        return View(product);
                    }

                    // Giữ nguyên ImageUrl nếu không có hình ảnh mới
                    if (imageUrl == null)
                    {
                        product.ImageUrl = existingProduct.ImageUrl;
                    }
                    else
                    {
                        product.ImageUrl = await SaveImage(imageUrl);
                    }

                    // Cập nhật thông tin sản phẩm
                    existingProduct.Name = product.Name;
                    existingProduct.Price = product.Price;
                    existingProduct.Description = product.Description;
                    existingProduct.CategoryId = product.CategoryId;
                    existingProduct.ImageUrl = product.ImageUrl;

                    await _productRepository.UpdateAsync(existingProduct);
                    return RedirectToAction(nameof(Index));
                }
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi khi cập nhật sản phẩm: " + ex.Message);
            }

            // Sử dụng lại categories đã khai báo
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