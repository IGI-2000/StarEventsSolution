using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarEvents.Models.Domain
{
    // Saved tokenized card entity (renamed to avoid collision with PaymentMethod enum)
    [Table("PaymentMethods")]
    public class StoredPaymentMethod
    {
        [Key]
        public int PaymentMethodId { get; set; }

        // token returned by gateway (no PAN)
        [Required, StringLength(200)]
        public string Token { get; set; }

        [Required, StringLength(10)]
        public string Last4 { get; set; }

        public int ExpiryMonth { get; set; }
        public int ExpiryYear { get; set; }

        [StringLength(50)]
        public string CardBrand { get; set; }

        // FK to Customer if you want to track who saved the card (0 = unknown)
        public int CustomerId { get; set; }

        [StringLength(100)]
        public string DisplayName { get; set; }
    }
}