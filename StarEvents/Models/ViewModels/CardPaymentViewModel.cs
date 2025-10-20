using System;
using System.ComponentModel.DataAnnotations;

namespace StarEvents.Models.ViewModels
{
    public class CardPaymentViewModel
    {
        [Required, StringLength(100)]
        public string NameOnCard { get; set; }

        [Required, StringLength(19)]
        public string CardNumber { get; set; } // will be tokenized; do not store raw PAN

        [Required]
        public int ExpiryMonth { get; set; }

        [Required]
        public int ExpiryYear { get; set; }

        [Required, StringLength(4)]
        public string CVV { get; set; }

        public bool SaveCard { get; set; } = false;
    }
}