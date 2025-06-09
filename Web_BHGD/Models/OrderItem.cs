namespace Web_BHGD.Models
{
    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public Order? Order { get; set; } // Có thể null

        public int ProductId { get; set; }
        public Product? Product { get; set; } // Có thể null

        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}