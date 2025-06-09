using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims; // Để lấy UserId
using Web_BHGD.Models;
using Web_BHGD; // <--- Bắt buộc để sử dụng SessionExtensions
using Web_BHGD.Models.Repositories;
using Microsoft.AspNetCore.Http; // Thêm cho SessionExtensions
using Newtonsoft.Json; // Thêm cho SessionExtensions

namespace Web_BanHang_T6S34.Controllers
{
    [Authorize] // Yêu cầu đăng nhập để truy cập Controller này
    public class OrderController : Controller
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IOrderItemRepository _orderItemRepository;
        private readonly IProductRepository _productRepository; // Cần để lấy thông tin sản phẩm

        public OrderController(IOrderRepository orderRepository, IOrderItemRepository orderItemRepository, IProductRepository productRepository)
        {
            _orderRepository = orderRepository;
            _orderItemRepository = orderItemRepository;
            _productRepository = productRepository;
        }

        // Hiển thị trang đặt hàng
        public IActionResult Checkout()
        {
            List<CartItem> cart = HttpContext.Session.GetJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            if (!cart.Any())
            {
                TempData["Error"] = "Giỏ hàng của bạn đang trống. Vui lòng thêm sản phẩm vào giỏ hàng trước khi thanh toán.";
                return RedirectToAction("Index", "Cart");
            }
            // Bạn có thể tạo một ViewModel ở đây để truyền thêm thông tin người dùng (địa chỉ, số điện thoại) vào View
            return View(cart);
        }

        // Xử lý khi người dùng nhấn nút "Đặt hàng"
        [HttpPost]
        public async Task<IActionResult> PlaceOrder(string shippingAddress) // Thêm tham số địa chỉ giao hàng
        {
            List<CartItem> cart = HttpContext.Session.GetJson<List<CartItem>>("Cart") ?? new List<CartItem>();
            if (!cart.Any())
            {
                TempData["Error"] = "Giỏ hàng của bạn đang trống.";
                return RedirectToAction("Index", "Cart");
            }

            // Lấy User ID của người dùng hiện tại
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                // Đây là trường hợp không mong muốn vì đã có [Authorize]
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }

            // Tạo đối tượng Order
            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.Now,
                TotalAmount = cart.Sum(item => item.Total),
                ShippingAddress = shippingAddress,
                Status = "Pending" // Trạng thái ban đầu của đơn hàng
            };

            await _orderRepository.Create(order); // Lưu Order vào DB để có Order.Id

            // Tạo danh sách OrderItem từ CartItem
            var orderItems = new List<OrderItem>();
            foreach (var item in cart)
            {
                var product = await _productRepository.GetById(item.ProductId); // Lấy lại thông tin sản phẩm
                if (product != null)
                {
                    orderItems.Add(new OrderItem
                    {
                        OrderId = order.Id,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        Price = product.Price // Sử dụng giá hiện tại của sản phẩm để lưu vào OrderItem
                    });
                }
            }

            await _orderItemRepository.CreateRange(orderItems); // Lưu tất cả OrderItem vào DB

            // Xóa giỏ hàng sau khi đặt hàng thành công
            HttpContext.Session.Remove("Cart");

            TempData["Success"] = "Đơn hàng của bạn đã được đặt thành công!";
            return RedirectToAction("OrderConfirmation", new { orderId = order.Id }); // Chuyển hướng đến trang xác nhận
        }

        // Hiển thị trang xác nhận đơn hàng
        public async Task<IActionResult> OrderConfirmation(int orderId)
        {
            var order = await _orderRepository.GetById(orderId);
            if (order == null || order.UserId != User.FindFirstValue(ClaimTypes.NameIdentifier))
            {
                return NotFound(); // Không tìm thấy đơn hàng hoặc không phải của người dùng này
            }
            return View(order);
        }

        // Hiển thị lịch sử đơn hàng của người dùng
        public async Task<IActionResult> OrderHistory()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Account/Login", new { area = "Identity" });
            }
            var orders = await _orderRepository.GetOrdersByUserId(userId);
            return View(orders);
        }
    }
}