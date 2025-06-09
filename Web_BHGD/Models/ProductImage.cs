using System.ComponentModel.DataAnnotations;

namespace Web_BHGD.Models
{
    public class ProductImage
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Đường dẫn ảnh không được để trống")] // Thêm Required
        public string Url { get; set; } = string.Empty; // Khởi tạo để tránh CS8618

        // Kết nối bảng Product
        public int ProductId { get; set; }
        public Product? Product { get; set; } // Có thể null
    }
}