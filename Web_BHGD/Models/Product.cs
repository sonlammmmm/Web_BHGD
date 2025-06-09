using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Web_BHGD.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm không được để trống")] // Thêm Required
        [StringLength(100, ErrorMessage = "Tên SP không được vượt quá 100 ký tự")]
        public string Name { get; set; } = string.Empty; // Khởi tạo để tránh CS8618

        [Range(0, 100000000, ErrorMessage = "Giá SP nằm từ 0 - 100000000")]
        public decimal Price { get; set; }

        public string? Description { get; set; } // Có thể null
        public string? ImageUrl { get; set; } // Có thể null

        // Kết nối bảng Category
        public int CategoryId { get; set; }
        public Category? Category { get; set; } // Có thể null

        // Kết nối bảng ProductImage
        public List<ProductImage>? Images { get; set; } // Có thể null
    }
}