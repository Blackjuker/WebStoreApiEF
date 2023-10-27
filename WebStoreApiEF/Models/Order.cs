using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace WebStoreApiEF.Models
{
    public class Order
    {
        public int Id { get; set; }
        public int UserId {  get; set; }
        public DateTime CreatedAt { get; set; }
        [Precision(16,2)]
        public decimal ShippingFee {  get; set; }
        [MaxLength(150)]
        public string DeliveryAddress { get; set; } = "";
        [MaxLength(30)]
        public string PaymentMethod { get; set; } = "";
        [MaxLength(30)]
        public string PaymentStatus { get; set; } = "";
        public string OrderStatus { get; set; } = "";

        //Navigations properties
        public User user { get; set; } = null!;
        public List<OrderItem> OrderItems { get; set; } = new();
    }
}
