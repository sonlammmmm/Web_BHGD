using Web_BHGD.Models;
using Web_BHGD.Repositories;
using Web_BHGD.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Web_BHGD.Controllers
{
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
            ViewBag.TotalAmount = cart.Items.Sum(i => i.Price * i.Quantity);
            ViewBag.TotalItems = cart.Items.Sum(i => i.Quantity);
            return View(cart);
        }

        // Thêm sản phẩm vào giỏ hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            if (productId <= 0)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "ID sản phẩm không hợp lệ." });
                }
                TempData["Error"] = "ID sản phẩm không hợp lệ.";
                return RedirectToAction("Index", "Product");
            }

            if (quantity <= 0 || quantity > 99)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Số lượng phải từ 1 đến 99." });
                }
                TempData["Error"] = "Số lượng phải từ 1 đến 99.";
                return RedirectToAction("Details", "Product", new { id = productId });
            }

            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại." });
                }
                TempData["Error"] = "Sản phẩm không tồn tại.";
                return RedirectToAction("Index", "Product");
            }

            try
            {
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

                // Trả về JSON cho AJAX request
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    var itemCount = cart.Items.Sum(i => i.Quantity);
                    return Json(new
                    {
                        success = true,
                        message = $"Đã thêm {product.Name} vào giỏ hàng.",
                        itemCount = itemCount
                    });
                }

                // Redirect cho request thường
                TempData["Success"] = $"Đã thêm {product.Name} vào giỏ hàng.";
                return RedirectToAction("Details", "Product", new { id = productId });
            }
            catch (Exception ex)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = $"Lỗi khi thêm sản phẩm: {ex.Message}" });
                }
                TempData["Error"] = $"Lỗi khi thêm sản phẩm: {ex.Message}";
                return RedirectToAction("Details", "Product", new { id = productId });
            }
        }

        [HttpGet]
        public IActionResult GetCartItemCount()
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart");
            var itemCount = cart?.Items.Sum(i => i.Quantity) ?? 0;
            return Json(new { count = itemCount });
        }

        // Cập nhật số lượng
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
                    var itemTotal = item.Price * item.Quantity; // Thành tiền của sản phẩm
                    var totalPrice = cart.Items.Sum(i => i.Price * i.Quantity); // Tổng tiền của giỏ hàng
                    return Json(new
                    {
                        success = true,
                        message = "Đã cập nhật số lượng sản phẩm.",
                        itemTotal = itemTotal.ToString("#,##0"), // Định dạng số
                        totalPrice = totalPrice.ToString("#,##0") // Định dạng số
                    });
                }
            }
            return Json(new { success = false, message = "Sản phẩm không tồn tại trong giỏ hàng." });
        }

        // Xóa sản phẩm
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
                    return Json(new { success = true, message = $"Đã xóa {item.Name} khỏi giỏ hàng." });
                }
            }
            return Json(new { success = false, message = "Sản phẩm không tồn tại trong giỏ hàng." });
        }

        // Xóa toàn bộ giỏ hàng
        public IActionResult ClearCart()
        {
            HttpContext.Session.Remove("Cart");
            return Json(new { success = true, message = "Đã xóa tất cả sản phẩm khỏi giỏ hàng." });
        }

        // Trang thanh toán
        public async Task<IActionResult> Checkout()
        {
            var cart = HttpContext.Session.GetObjectFromJson<ShoppingCart>("Cart");
            if (cart == null || !cart.Items.Any())
            {
                TempData["Error"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction("Index");
            }
            
            var order = new Order
            {
                OrderDate = DateTime.UtcNow,
                TotalPrice = cart.Items.Sum(i => i.Price * i.Quantity)
            };

            // Điền sẵn thông tin cho người dùng đã đăng nhập
            if (User.Identity.IsAuthenticated)
            {
                var user = await _userManager.GetUserAsync(User);
                order.UserId = user.Id;
                order.CustomerName = user.UserName;
                order.CustomerEmail = user.Email;
                order.CustomerPhone = user.PhoneNumber ?? "";
            }

            ViewBag.TotalAmount = cart.Items.Sum(i => i.Price * i.Quantity);
            ViewBag.CartItems = cart.Items;
            return View(order);
        }

        // Xử lý đặt hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
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
                // Xử lý UserId
                if (User.Identity.IsAuthenticated)
                {
                    var user = await _userManager.GetUserAsync(User);
                    order.UserId = user.Id;
                }
                else
                {
                    order.UserId = null; // Khách vãng lai
                }

                order.OrderDate = DateTime.UtcNow;
                order.TotalPrice = cart.Items.Sum(i => i.Price * i.Quantity);
                order.Status = "Pending"; // Trạng thái mặc định
                order.OrderDetails = cart.Items.Select(i => new OrderDetail
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    Price = i.Price
                }).ToList();

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Xóa giỏ hàng
                HttpContext.Session.Remove("Cart");
                TempData["Success"] = "Đặt hàng thành công! Đơn hàng đang chờ xác nhận từ quản trị viên.";
                return View("OrderCompleted", order.Id);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Có lỗi xảy ra khi đặt hàng: {ex.Message}";
                ViewBag.TotalAmount = cart.Items.Sum(i => i.Price * i.Quantity);
                ViewBag.CartItems = cart.Items;
                return View(order);
            }
        }

        private async Task<Product> GetProductFromDatabase(int productId)
        {
            return await _productRepository.GetByIdAsync(productId);
        }
    }
}