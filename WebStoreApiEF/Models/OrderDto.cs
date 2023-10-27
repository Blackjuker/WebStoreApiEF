using System.ComponentModel.DataAnnotations;

namespace WebStoreApiEF.Models
{
    public class OrderDto
    {
        [Required]
        public string ProductIdentifiers { get; set; } = "";
        [Required,MinLength(30),MaxLength(150)]
        public string DeliveryAddress { get; set; } = "";
        [Required]
        public string PaymentMethod { get; set; } = "";
    }
}
