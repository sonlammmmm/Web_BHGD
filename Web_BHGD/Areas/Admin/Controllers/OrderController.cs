using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web_BHGD.Models;

namespace Web_BHGD.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrderController(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // Hiển thị danh sách đơn hàng
        public async Task<IActionResult> Index()
        {
            if (_context.Orders == null)
            {
                TempData["Error"] = "Không thể truy cập dữ liệu đơn hàng.";
                return View(new List<Order>());
            }

            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Include(o => o.ApplicationUser)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return View(orders);
        }

        // Xem chi tiết đơn hàng
        public async Task<IActionResult> Details(int id)
        {
            if (_context.Orders == null)
            {
                TempData["Error"] = "Không thể truy cập dữ liệu đơn hàng.";
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Include(o => o.ApplicationUser)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                TempData["Error"] = $"Không tìm thấy đơn hàng #{id}.";
                return NotFound();
            }

            return View(order);
        }

        // Xác nhận đơn hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmOrder(int id)
        {
            if (_context.Orders == null)
            {
                TempData["Error"] = "Không thể truy cập dữ liệu đơn hàng.";
                return NotFound();
            }

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                TempData["Error"] = $"Không tìm thấy đơn hàng #{id}.";
                return NotFound();
            }

            order.Status = "Confirmed";
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã xác nhận đơn hàng #{id}";
            return RedirectToAction("Index");
        }

        // Cập nhật trạng thái đơn hàng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status)
        {
            if (_context.Orders == null)
            {
                TempData["Error"] = "Không thể truy cập dữ liệu đơn hàng.";
                return NotFound();
            }

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                TempData["Error"] = $"Không tìm thấy đơn hàng #{id}.";
                return NotFound();
            }

            if (!new[] { "Pending", "Confirmed", "Cancelled" }.Contains(status))
            {
                TempData["Error"] = "Trạng thái không hợp lệ.";
                return RedirectToAction("Details", new { id });
            }

            order.Status = status;
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã cập nhật trạng thái đơn hàng #{id} thành {GetStatusText(status)}.";
            return RedirectToAction("Details", new { id });
        }

        // Tạo hóa đơn HTML
        public async Task<IActionResult> GenerateInvoice(int id)
        {
            if (_context.Orders == null)
            {
                TempData["Error"] = "Không thể truy cập dữ liệu đơn hàng.";
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Include(o => o.ApplicationUser)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                TempData["Error"] = $"Không tìm thấy đơn hàng #{id}.";
                return NotFound();
            }

            return View("Invoice", order);
        }

        private string GetStatusText(string status)
        {
            return status switch
            {
                "Pending" => "Chưa thanh toán",
                "Confirmed" => "Đã thanh toán",
                "Cancelled" => "Đã hủy",
                _ => status
            };
        }
    }
}