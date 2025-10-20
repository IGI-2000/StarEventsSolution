using System;
using System.ComponentModel.DataAnnotations;

namespace StarEvents.Models.ViewModels
{
    public class PaymentViewModel
    {
        public int BookingId { get; set; }

        // New: booking reference shown on payment page
        public string BookingReference { get; set; }

        [Required]
        public decimal Amount { get; set; }

        // Customer contact (used for receipts)
        [Required, EmailAddress]
        public string CustomerEmail { get; set; }

        // Card fields (transient only — never persist CVV / PAN)
        [Required, StringLength(100)]
        public string NameOnCard { get; set; }

        [Required, StringLength(19)]
        public string CardNumber { get; set; }

        [Required]
        public int ExpiryMonth { get; set; }

        [Required]
        public int ExpiryYear { get; set; }

        [Required, StringLength(4)]
        public string CVV { get; set; }

        // Save tokenized card for future use (stores token only)
        public bool SaveCard { get; set; } = false;

        // Optional: Payment method label
        public string Method { get; set; }
    }
}