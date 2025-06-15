using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Web_BHGD.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string? UserId { get; set; } // Null cho người dùng không đăng nhập
        public DateTime OrderDate { get; set; }
        [Range(0, 999999999, ErrorMessage = "Tổng giá phải nằm trong khoảng từ 0 đến 999,999,999 VNĐ")]
        public decimal TotalPrice { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng")]
        public string ShippingAddress { get; set; }
        public string? Notes { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập tên người nhận")]
        public string CustomerName { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string CustomerPhone { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string CustomerEmail { get; set; }
        [Required(ErrorMessage = "Vui lòng chọn trạng thái đơn hàng")]
        public string Status { get; set; } = "Pending"; // Trạng thái mặc định là Pending
        [ForeignKey("UserId")]
        [ValidateNever]
        public ApplicationUser? ApplicationUser { get; set; }
        public List<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
    }
}