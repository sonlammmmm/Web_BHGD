using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web_BHGD.Models;
using Web_BHGD.Models.Repositories;
using System.Security.Claims; // Để lấy UserId
using Microsoft.AspNetCore.Http; // Thêm cho SessionExtensions
using Newtonsoft.Json; // Thêm cho SessionExtensions

namespace Web_BHGD.Controllers
{
    [Authorize] // Yêu cầu đăng nhập để truy cập Controller này
    public class CartController : Controller
    {
        private readonly IProductRepository _productRepository;

        public CartController(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        // Action để hiển thị giỏ hàng
        public IActionResult Index()
        {
            // Lấy giỏ hàng từ Session
            List<CartItem> cart = HttpContext.Session.GetJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            return View(cart);
        }

        // Action để thêm sản phẩm vào giỏ hàng
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            var product = await _productRepository.GetById(productId);
            if (product == null)
            {
                return NotFound();
            }

            List<CartItem> cart = HttpContext.Session.GetJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            var existingItem = cart.FirstOrDefault(item => item.ProductId == productId);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Price = product.Price,
                    Quantity = quantity,
                    ImageUrl = product.ImageUrl
                });
            }
            HttpContext.Session.SetJson("Cart", cart);

            TempData["Message"] = "Sản phẩm đã được thêm vào giỏ hàng!";
            return RedirectToAction("Index", "Product"); // Chuyển hướng về trang danh sách sản phẩm
        }

        // Action để xóa một mặt hàng khỏi giỏ hàng
        [HttpPost]
        public IActionResult RemoveFromCart(int productId)
        {
            List<CartItem> cart = HttpContext.Session.GetJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            var itemToRemove = cart.FirstOrDefault(item => item.ProductId == productId);
            if (itemToRemove != null)
            {
                cart.Remove(itemToRemove);
                HttpContext.Session.SetJson("Cart", cart);
                TempData["Message"] = "Sản phẩm đã được xóa khỏi giỏ hàng.";
            }
            return RedirectToAction("Index");
        }

        // Action để cập nhật số lượng sản phẩm trong giỏ hàng
        [HttpPost]
        public IActionResult UpdateQuantity(int productId, int quantity)
        {
            List<CartItem> cart = HttpContext.Session.GetJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            var itemToUpdate = cart.FirstOrDefault(item => item.ProductId == productId);
            if (itemToUpdate != null)
            {
                if (quantity > 0)
                {
                    itemToUpdate.Quantity = quantity;
                }
                else
                {
                    cart.Remove(itemToUpdate); // Nếu số lượng là 0 hoặc âm thì xóa khỏi giỏ hàng
                }
                HttpContext.Session.SetJson("Cart", cart);
                TempData["Message"] = "Số lượng sản phẩm đã được cập nhật.";
            }
            return RedirectToAction("Index");
        }
    }
}