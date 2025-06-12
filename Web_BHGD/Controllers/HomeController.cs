using System.Diagnostics;
using Web_BHGD.Models;
using Web_BHGD.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Web_BHGD.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;

        public HomeController(ILogger<HomeController> logger,
                            IProductRepository productRepository,
                            ICategoryRepository categoryRepository)
        {
            _logger = logger;
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
        }

        public async Task<IActionResult> Index()
        {
            // Lấy sản phẩm nổi bật (có thể là sản phẩm mới nhất, bán chạy, v.v.)
            var allProducts = await _productRepository.GetAllAsync();
            var featuredProducts = allProducts.Take(8).ToList(); // Hiển thị 8 sản phẩm đầu tiên

            // Lấy danh mục để hiển thị menu
            var categories = await _categoryRepository.GetAllAsync();

            ViewBag.Categories = categories;
            ViewBag.FeaturedProducts = featuredProducts;

            return View(allProducts.ToList());
        }

        // Trang giới thiệu về cửa hàng
        public IActionResult About()
        {
            return View();
        }

        // Trang liên hệ
        public IActionResult Contact()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        // Tìm kiếm nhanh trên trang chủ
        [HttpPost]
        public async Task<IActionResult> QuickSearch(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return RedirectToAction("Index");
            }

            var products = await _productRepository.GetAllAsync();
            var searchResults = products
                .Where(p => p.Name.ToLower().Contains(searchTerm.ToLower()) ||
                           p.Description.ToLower().Contains(searchTerm.ToLower()))
                .ToList();

            ViewBag.SearchTerm = searchTerm;
            ViewBag.SearchResults = searchResults;

            return View("SearchResults", searchResults);
        }
    }
}