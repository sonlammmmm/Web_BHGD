using Web_BHGD.Models;
using Web_BHGD.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Web_BHGD.Controllers
{
    public class CategoryController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;

        public CategoryController(IProductRepository productRepository, ICategoryRepository categoryRepository)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
        }

        // Hiển thị danh sách danh mục cho khách hàng
        public async Task<IActionResult> Index()
        {
            var categories = await _categoryRepository.GetAllAsync();

            // Tính số lượng sản phẩm cho mỗi danh mục
            var products = await _productRepository.GetAllAsync();
            var categoriesWithCount = categories.Select(c => new
            {
                Category = c,
                ProductCount = products.Count(p => p.CategoryId == c.Id)
            }).ToList();

            ViewBag.CategoriesWithCount = categoriesWithCount;
            return View(categories);
        }

        // Hiển thị chi tiết danh mục và sản phẩm thuộc danh mục đó
        public async Task<IActionResult> Details(int id, string sortOrder)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            // Lấy tất cả sản phẩm thuộc danh mục này
            var allProducts = await _productRepository.GetAllAsync();
            var products = allProducts.Where(p => p.CategoryId == id);

            // Sắp xếp sản phẩm
            products = sortOrder switch
            {
                "name_desc" => products.OrderByDescending(p => p.Name),
                "price" => products.OrderBy(p => p.Price),
                "price_desc" => products.OrderByDescending(p => p.Price),
                _ => products.OrderBy(p => p.Name),
            };

            ViewBag.CurrentSort = sortOrder;
            ViewBag.Products = products.ToList();

            return View(category);
        }

        // API để lấy danh sách danh mục (dùng cho dropdown, menu, v.v.)
        [HttpGet]
        public async Task<IActionResult> GetAllCategories()
        {
            var categories = await _categoryRepository.GetAllAsync();
            return Json(categories.Select(c => new {
                id = c.Id,
                name = c.Name
            }));
        }

        // Hiển thị menu danh mục (partial view)
        public async Task<IActionResult> CategoryMenu()
        {
            var categories = await _categoryRepository.GetAllAsync();
            return PartialView("_CategoryMenu", categories);
        }
    }
}