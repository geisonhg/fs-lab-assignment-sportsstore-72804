using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportsStore.Models {

    public class Order {
        public int OrderId { get; set; }

        [Required(ErrorMessage = "Please enter your name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter the first address line")]
        public string Line1 { get; set; } = string.Empty;

        public string? Line2 { get; set; }

        [Required(ErrorMessage = "Please enter a city name")]
        public string City { get; set; } = string.Empty;

        public string? State { get; set; }

        [Required(ErrorMessage = "Please enter a postal code")]
        public string Zip { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter a country name")]
        public string Country { get; set; } = string.Empty;

        public bool GiftWrap { get; set; }

        public string? StripeSessionId { get; set; }
        public string? PaymentIntentId { get; set; }
        public bool Dispatched { get; set; }

        public ICollection<OrderLine> Lines { get; set; } = new List<OrderLine>();
    }

    public class OrderLine {
        public int OrderLineId { get; set; }
        public int OrderId { get; set; }

        public long? ProductId { get; set; }
        public int Quantity { get; set; }

        [Column(TypeName = "decimal(8,2)")]
        public decimal UnitPrice { get; set; }

        public Product? Product { get; set; }
    }
}
