using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace StarEvents.Models.Domain
{
    [Table("Payments")]
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        [Column(TypeName = "decimal")]
        public decimal Amount { get; set; }

        // transaction id from gateway
        [StringLength(200)]
        public string TransactionId { get; set; }

        // date/time of payment
        public DateTime PaymentDate { get; set; }

        // generic gateway text response
        [StringLength(1000)]
        public string PaymentGatewayResponse { get; set; }

        [StringLength(10)]
        public string Last4 { get; set; }

        // FK to Booking (required by ApplicationDbContext mapping)
        public int BookingId { get; set; }

        // Navigation to Booking
        public virtual Booking Booking { get; set; }

        // Optional stored/tokenized card reference (if used)
        public int? PaymentMethodId { get; set; }

        // Navigation
        [ForeignKey("PaymentMethodId")]
        public virtual StoredPaymentMethod StoredPaymentMethod { get; set; }

        // Optional: payment method enum (if present)
        public PaymentMethod Method { get; set; }

        public PaymentStatus Status { get; set; }
    }
}
