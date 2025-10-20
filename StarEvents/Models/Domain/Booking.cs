using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarEvents.Models.Domain
{
    [Table("Booking")]
    public class Booking
    {
        // ----------------------------
        // Primary Key
        // ----------------------------
        [Key]
        public int BookingId { get; set; }

        // ----------------------------
        // Core Booking Info
        // ----------------------------
        [Required, StringLength(50)]
        public string BookingReference { get; set; }

        [Required]
        public DateTime BookingDate { get; set; }

        [Required]
        public BookingStatus Status { get; set; }

        // ----------------------------
        // Financial Details
        // ----------------------------
        [Column(TypeName = "decimal")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal")]
        public decimal DiscountAmount { get; set; }

        [Column(TypeName = "decimal")]
        public decimal FinalAmount { get; set; }

        // ----------------------------
        // Loyalty & Rewards
        // ----------------------------
        public int? LoyaltyPointsEarned { get; set; }
        public int? LoyaltyPointsUsed { get; set; }

        // ----------------------------
        // Foreign Keys
        // ----------------------------
        [Required]
        public int CustomerId { get; set; }

        [Required]
        public int EventId { get; set; }

        // Note: intentionally omitted PaymentId to avoid circular FK
        // public int? PaymentId { get; set; }

        // ----------------------------
        // Navigation Properties
        // ----------------------------
        public virtual Customer Customer { get; set; }
        public virtual Event Event { get; set; }

        // Navigation collections
        public virtual ICollection<BookingDetail> BookingDetails { get; set; } = new HashSet<BookingDetail>();
        public virtual ICollection<Ticket> Tickets { get; set; } = new HashSet<Ticket>();

        // ----------------------------
        // Derived / Convenience Members
        // ----------------------------
        [NotMapped]
        public bool IsConfirmed => Status == BookingStatus.Confirmed;

        [NotMapped]
        public bool IsCancelled => Status == BookingStatus.Cancelled;

        [NotMapped]
        public bool IsPending => Status == BookingStatus.Pending;

        [NotMapped]
        public string FinalAmountDisplay => FinalAmount.ToString("C");
    }
}
