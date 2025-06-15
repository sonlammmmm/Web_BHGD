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

        public async Task<IActionResult> Index(int? categoryId, string searchString, string sortOrder, int page = 1)
        {
            const int pageSize = 12; // 12 sản phẩm mỗi trang

            // Lấy sản phẩm với lọc, sắp xếp và phân trang
            var (products, totalCount) = await _productRepository.GetFilteredAndPagedAsync(
                categoryId, searchString, sortOrder, page, pageSize);

            // Chuẩn bị dữ liệu cho filter
            var categories = await _categoryRepository.GetAllAsync();

            // SelectList cho dropdown danh mục
            var categoryList = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "Tất cả danh mục" }
            };
            categoryList.AddRange(categories.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Name
            }));
            ViewBag.CategorySelectList = new SelectList(categoryList, "Value", "Text", categoryId?.ToString());

            // Danh sách danh mục cho _CategoryMenu
            ViewBag.CategoryList = categories;

            // Các ViewBag khác
            ViewBag.CurrentFilter = searchString;
            ViewBag.CurrentSort = sortOrder;
            ViewBag.CurrentCategory = categoryId;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

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
            ViewBag.RelatedProducts = await _productRepository.GetRelatedProductsAsync(product.CategoryId, product.Id, 4);

            return View("Details", product);
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