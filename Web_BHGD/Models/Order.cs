using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Web_BHGD.Models
{
    public class Order
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty; // Khởi tạo để tránh CS8618
        public DateTime OrderDate { get; set; } = DateTime.Now;
        public decimal TotalAmount { get; set; }
        public string? ShippingAddress { get; set; } // Có thể null
        public string? Status { get; set; } = "Pending";

        [ForeignKey("UserId")]
        public IdentityUser? User { get; set; } // Có thể null

        public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>(); // Khởi tạo để tránh CS8618 và CS8620
    }
}