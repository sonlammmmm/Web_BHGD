namespace Web_BHGD.Models
{
    public class CartItem
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty; // Khởi tạo để tránh CS8618
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string? ImageUrl { get; set; } // Có thể null
        public decimal Total => Price * Quantity;
    }
}