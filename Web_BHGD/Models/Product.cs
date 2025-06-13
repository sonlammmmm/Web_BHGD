using System.ComponentModel.DataAnnotations;

namespace Web_BHGD.Models
{
    public class Product
    {
        public int Id { get; set; }
        [Required, StringLength(100)]
        public string Name { get; set; }

        [Range(1, 999999999, ErrorMessage = "Giá phải nằm trong khoảng từ 1 đến 999,999,999 VNĐ")]
        public decimal Price { get; set; }
        public string Description { get; set; }
        public string? ImageUrl { get; set; }
        public List<ProductImage>? Images { get; set; }
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
    }
}
