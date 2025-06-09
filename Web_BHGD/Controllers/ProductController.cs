using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Web_BHGD.Models;
using Web_BHGD.Models.Repositories;
using Microsoft.AspNetCore.Authorization; // <-- Đảm bảo có dòng này

namespace Web_BHGD.Controllers
{
    public class ProductController : Controller
    {
        private IProductRepository _productRepository;
        private ICategoryRepository _categoryRepository;

        public ProductController(IProductRepository productRepository, ICategoryRepository categoryRepository)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
        }

        // Action này chỉ Admin mới được truy cập (danh sách quản lý)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var list_Product = await _productRepository.GetAll();
            return View(list_Product);
        }

        // Action Details (Chi tiết) sẽ công khai, không cần thuộc tính [Authorize]
        public async Task<IActionResult> Details(int id)
        {
            var product = await _productRepository.GetById(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        // Các action Create, Edit, Delete vẫn cần quyền Admin
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create()
        {
            var categories = await _categoryRepository.GetAll();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")] // Thêm thuộc tính này
        public async Task<IActionResult> Create(Product product, IFormFile? file_Image)
        {
            // ... (code Create của bạn) ...
            if (ModelState.IsValid)
            {
                if (file_Image != null && file_Image.Length > 0)
                {
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + file_Image.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await file_Image.CopyToAsync(fileStream);
                    }
                    product.ImageUrl = "images/" + uniqueFileName;
                }

                await _productRepository.Create(product);
                return RedirectToAction(nameof(Index));
            }

            var categories = await _categoryRepository.GetAll();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        [Authorize(Roles = "Admin")] // Thêm thuộc tính này
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _productRepository.GetById(id);
            if (product == null)
            {
                return NotFound();
            }
            var categories = await _categoryRepository.GetAll();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")] // Thêm thuộc tính này
        public async Task<IActionResult> Edit(int id, Product product, IFormFile? file_Image)
        {
            if (id != product.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                if (file_Image != null && file_Image.Length > 0)
                {
                    string uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + file_Image.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    if (!string.IsNullOrEmpty(product.ImageUrl))
                    {
                        string oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", product.ImageUrl);
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await file_Image.CopyToAsync(fileStream);
                    }
                    product.ImageUrl = "images/" + uniqueFileName;
                }

                await _productRepository.Update(product);
                return RedirectToAction(nameof(Index));
            }
            var categories = await _categoryRepository.GetAll();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", product.CategoryId);
            return View(product);
        }

        [Authorize(Roles = "Admin")] // Thêm thuộc tính này
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _productRepository.GetById(id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")] // Thêm thuộc tính này
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _productRepository.GetById(id);
            if (product == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(product.ImageUrl))
            {
                string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", product.ImageUrl);
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            await _productRepository.Delete(id);
            return RedirectToAction(nameof(Index));
        }
    }
}