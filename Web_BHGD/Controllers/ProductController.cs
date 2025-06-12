using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Web_BHGD.Models;
using Web_BHGD.Repositories;

namespace Web_BHGD.Controllers
{
    public class ProductController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;

        public ProductController(IProductRepository productRepository, ICategoryRepository categoryRepository)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
        }

        // Hiển thị danh sách sản phẩm cho khách hàng
        public async Task<IActionResult> Index(int? categoryId, string searchString, string sortOrder)
        {
            var products = await _productRepository.GetAllAsync();

            // Lọc theo danh mục
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                products = products.Where(p => p.CategoryId == categoryId.Value);
            }

            // Tìm kiếm theo tên sản phẩm
            if (!string.IsNullOrEmpty(searchString))
            {
                products = products.Where(p => p.Name.ToLower().Contains(searchString.ToLower()));
            }

            // Sắp xếp
            products = sortOrder switch
            {
                "name_desc" => products.OrderByDescending(p => p.Name),
                "price" => products.OrderBy(p => p.Price),
                "price_desc" => products.OrderByDescending(p => p.Price),
                _ => products.OrderBy(p => p.Name),
            };

            // Chuẩn bị dữ liệu cho filter
            var categories = await _categoryRepository.GetAllAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", categoryId);
            ViewBag.CurrentFilter = searchString;
            ViewBag.CurrentSort = sortOrder;
            ViewBag.CurrentCategory = categoryId;

            return View(products);
        }

        // Hiển thị thông tin chi tiết sản phẩm cho khách hàng
        public async Task<IActionResult> Details(int id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            // Lấy danh sách sản phẩm liên quan (cùng danh mục)
            var relatedProducts = await _productRepository.GetAllAsync();
            ViewBag.RelatedProducts = relatedProducts
                .Where(p => p.CategoryId == product.CategoryId && p.Id != id)
                .Take(4)
                .ToList();

            return View(product);
        }

        // API để lấy sản phẩm theo danh mục (dùng cho Ajax)
        [HttpGet]
        public async Task<IActionResult> GetProductsByCategory(int categoryId)
        {
            var products = await _productRepository.GetAllAsync();
            var filteredProducts = products.Where(p => p.CategoryId == categoryId).ToList();

            return Json(filteredProducts.Select(p => new {
                id = p.Id,
                name = p.Name,
                price = p.Price,
                imageUrl = p.ImageUrl,
                description = p.Description
            }));
        }

        // Tìm kiếm sản phẩm (API cho Ajax)
        [HttpGet]
        public async Task<IActionResult> Search(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                return Json(new List<object>());
            }

            var products = await _productRepository.GetAllAsync();
            var searchResults = products
                .Where(p => p.Name.ToLower().Contains(query.ToLower()) ||
                           p.Description.ToLower().Contains(query.ToLower()))
                .Take(10)
                .ToList();

            return Json(searchResults.Select(p => new {
                id = p.Id,
                name = p.Name,
                price = p.Price,
                imageUrl = p.ImageUrl
            }));
        }
    }
}