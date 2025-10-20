// file: StarEvents\Models\Domain\BookingDetail.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarEvents.Models.Domain
{
    public class BookingDetail
    {
        // ----------------------------
        // Primary Key
        // ----------------------------
        [Key]
        public int BookingDetailId { get; set; }

        // ----------------------------
        // Quantity and Pricing
        // ----------------------------
        public int Quantity { get; set; }

        [Column(TypeName = "decimal")]
        public decimal UnitPrice { get; set; }

        [Column(TypeName = "decimal")]
        public decimal Subtotal { get; set; }

        // ----------------------------
        // Foreign Keys
        // ----------------------------
        public int BookingId { get; set; }
        public int TicketTypeId { get; set; }

        // ----------------------------
        // Navigation Properties
        // ----------------------------
        public virtual Booking Booking { get; set; }
        public virtual TicketType TicketType { get; set; }
    }
}
