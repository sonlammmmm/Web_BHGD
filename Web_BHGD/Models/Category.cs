using System.Collections.Generic;
using System.ComponentModel.DataAnnotations; // Thêm dòng này cho Required

namespace Web_BHGD.Models
{
    public class Category
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên danh mục không được để trống")] // Thêm Required
        public string Name { get; set; } = string.Empty; // Khởi tạo để tránh CS8618

        // Kết nối với bảng Product - Cho phép nullable
        public List<Product>? Products { get; set; }
    }
}