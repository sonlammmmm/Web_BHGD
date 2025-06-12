using Web_BHGD.Models;
using Web_BHGD.Repositories;
using Web_BHGD.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Web_BHGD.Controllers
{
    [Authorize] // Chỉ user đã đăng nhập mới có thể sử dụng giỏ hàng
    public class ShoppingCartController : Controller
    {
        private readonly IProductRepository _productRepository;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ShoppingCartController(ApplicationDbContext context,
                                    UserManager<ApplicationUser> userManager,
                                    IProductRepository productRepository)
        {
            _productRepository = productRepository;
            _context = context;
            _userManager = userManager;
        }

        // Hiển thị giỏ hàng
        public IActionResult Index()
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart") ?? new ShoppingCart();

            // Tính tổng tiền
            ViewBag.TotalAmount = cart.Items.Sum(i => i.Price * i.Quantity);
            ViewBag.TotalItems = cart.Items.Sum(i => i.Quantity);

            return View(cart);
        }

        // Thêm sản phẩm vào giỏ hàng
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            if (quantity <= 0)
            {
                TempData["Error"] = "Số lượng phải lớn hơn 0.";
                return RedirectToAction("Index", "Home");
            }

            var product = await GetProductFromDatabase(productId);
            if (product == null)
            {
                TempData["Error"] = "Sản phẩm không tồn tại.";
                return RedirectToAction("Index", "Home");
            }

            var cartItem = new CartItem
            {
                ProductId = productId,
                Name = product.Name,
                Price = product.Price,
                Quantity = quantity
            };

            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart") ?? new ShoppingCart();
            cart.AddItem(cartItem);
            HttpContext.Session.SetObjectAsJson("Cart", cart);

            TempData["Success"] = $"Đã thêm {product.Name} vào giỏ hàng.";
            return RedirectToAction("Index", "Home");
        }

        // Cập nhật số lượng sản phẩm trong giỏ hàng
        [HttpPost]
        public IActionResult UpdateQuantity(int productId, int quantity)
        {
            if (quantity <= 0)
            {
                return RemoveFromCart(productId);
            }

            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart");
            if (cart != null)
            {
                var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
                if (item != null)
                {
                    item.Quantity = quantity;
                    HttpContext.Session.SetObjectAsJson("Cart", cart);
                    TempData["Success"] = "Đã cập nhật số lượng sản phẩm.";
                }
            }

            return RedirectToAction("Index");
        }

        // Xóa sản phẩm khỏi giỏ hàng
        public IActionResult RemoveFromCart(int productId)
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart");
            if (cart != null)
            {
                var item = cart.Items.FirstOrDefault(i => i.ProductId == productId);
                if (item != null)
                {
                    cart.RemoveItem(productId);
                    HttpContext.Session.SetObjectAsJson("Cart", cart);
                    TempData["Success"] = $"Đã xóa {item.Name} khỏi giỏ hàng.";
                }
            }
            return RedirectToAction("Index");
        }

        // Xóa tất cả sản phẩm trong giỏ hàng
        public IActionResult ClearCart()
        {
            HttpContext.Session.Remove("Cart");
            TempData["Success"] = "Đã xóa tất cả sản phẩm khỏi giỏ hàng.";
            return RedirectToAction("Index");
        }

        // Trang thanh toán
        public IActionResult Checkout()
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart");
            if (cart == null || !cart.Items.Any())
            {
                TempData["Error"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction("Index");
            }

            var order = new Order();
            ViewBag.TotalAmount = cart.Items.Sum(i => i.Price * i.Quantity);
            ViewBag.CartItems = cart.Items;

            return View(order);
        }

        // Xử lý đặt hàng
        [HttpPost]
        public async Task<IActionResult> Checkout(Order order)
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart");
            if (cart == null || !cart.Items.Any())
            {
                TempData["Error"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction("Index");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.TotalAmount = cart.Items.Sum(i => i.Price * i.Quantity);
                ViewBag.CartItems = cart.Items;
                return View(order);
            }

            try
            {
                var user = await _userManager.GetUserAsync(User);
                order.UserId = user.Id;
                order.OrderDate = DateTime.UtcNow;
                order.TotalPrice = cart.Items.Sum(i => i.Price * i.Quantity);
                order.OrderDetails = cart.Items.Select(i => new OrderDetail
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    Price = i.Price
                }).ToList();

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Xóa giỏ hàng sau khi đặt hàng thành công
                HttpContext.Session.Remove("Cart");

                TempData["Success"] = "Đặt hàng thành công!";
                return View("OrderCompleted", order.Id);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra khi đặt hàng. Vui lòng thử lại.";
                ViewBag.TotalAmount = cart.Items.Sum(i => i.Price * i.Quantity);
                ViewBag.CartItems = cart.Items;
                return View(order);
            }
        }

        // Lấy số lượng sản phẩm trong giỏ hàng (dùng cho hiển thị trên layout)
        [HttpGet]
        public IActionResult GetCartItemCount()
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart");
            var itemCount = cart?.Items.Sum(i => i.Quantity) ?? 0;
            return Json(itemCount);
        }

        // Private methods
        private async Task<Product> GetProductFromDatabase(int productId)
        {
            return await _productRepository.GetByIdAsync(productId);
        }
    }
}