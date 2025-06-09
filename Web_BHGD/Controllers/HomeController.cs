using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Web_BHGD.Models;
using Web_BHGD.Models.Repositories;

namespace Web_BHGD.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IProductRepository _productRepository; // Khai báo ProductRepository

        public HomeController(ILogger<HomeController> logger, IProductRepository productRepository) // Thêm IProductRepository vào constructor
        {
            _logger = logger;
            _productRepository = productRepository; // Gán giá trị
        }

        public async Task<IActionResult> Index() // Thay đổi sang async Task<IActionResult>
        {
            var products = await _productRepository.GetAll(); // Lấy tất cả sản phẩm
            return View(products); // Truyền danh sách sản phẩm vào View
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
    }
}