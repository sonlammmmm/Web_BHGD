using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.IO;
using Web_BHGD.Models;
using Web_BHGD.Repositories;

namespace Web_BHGD.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepo;
        private readonly ICategoryRepository _categoryRepo;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductController(
            IProductRepository productRepo,
            ICategoryRepository categoryRepo,
            IWebHostEnvironment webHostEnvironment)
        {
            _productRepo = productRepo;
            _categoryRepo = categoryRepo;
            _webHostEnvironment = webHostEnvironment;
        }

        // Hiển thị danh sách sản phẩm
        public async Task<IActionResult> Index()
        {
            var products = await _productRepo.GetAllAsync();
            return View(products);
        }

        // Hiển thị form thêm mới
        [HttpGet]
        public async Task<IActionResult> Add()
        {
            ViewBag.Categories = new SelectList(await _categoryRepo.GetAllAsync(), "Id", "Name");
            return View();
        }

        // Xử lý thêm sản phẩm
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(Product product, IFormFile imageFile)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = new SelectList(await _categoryRepo.GetAllAsync(), "Id", "Name", product.CategoryId);
                return View(product);
            }

            if (imageFile != null && imageFile.Length > 0)
            {
                product.ImageUrl = await SaveImageAsync(imageFile);
            }

            await _productRepo.AddAsync(product);
            TempData["Success"] = "Đã thêm sản phẩm thành công!";
            return RedirectToAction(nameof(Index));
        }

        // Hiển thị form cập nhật
        [HttpGet]
        public async Task<IActionResult> Update(int id)
        {
            var product = await _productRepo.GetByIdAsync(id);
            if (product == null) return NotFound();

            ViewBag.Categories = new SelectList(await _categoryRepo.GetAllAsync(), "Id", "Name", product.CategoryId);
            return View(product);
        }

        // Xử lý cập nhật
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(Product product, IFormFile imageFile)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = new SelectList(await _categoryRepo.GetAllAsync(), "Id", "Name", product.CategoryId);
                return View(product);
            }

            if (imageFile != null && imageFile.Length > 0)
            {
                if (!string.IsNullOrEmpty(product.ImageUrl))
                    DeleteImage(product.ImageUrl);

                product.ImageUrl = await SaveImageAsync(imageFile);
            }

            await _productRepo.UpdateAsync(product);
            TempData["Success"] = "Cập nhật thành công!";
            return RedirectToAction(nameof(Index));
        }

        // Xác nhận xoá
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _productRepo.GetByIdAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        // Xử lý xoá
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _productRepo.GetByIdAsync(id);
            if (product == null) return NotFound();

            if (!string.IsNullOrEmpty(product.ImageUrl))
                DeleteImage(product.ImageUrl);

            await _productRepo.DeleteAsync(id);
            TempData["Success"] = "Đã xoá sản phẩm.";
            return RedirectToAction(nameof(Index));
        }

        // Chi tiết sản phẩm
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var product = await _productRepo.GetByIdAsync(id);
            if (product == null) return NotFound();
            return View(product);
        }

        // Lưu ảnh duy nhất vào wwwroot/images
        private async Task<string> SaveImageAsync(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
                return null;

            var uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }

            return "/images/products/" + uniqueFileName;
        }

        // Xoá ảnh khỏi wwwroot/images
        private void DeleteImage(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return;

            var imagePath = Path.Combine(_webHostEnvironment.WebRootPath, imageUrl.TrimStart('/'));
            if (System.IO.File.Exists(imagePath))
            {
                System.IO.File.Delete(imagePath);
            }
        }
    }
}
