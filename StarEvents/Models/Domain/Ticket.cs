using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarEvents.Models.Domain
{
    [Table("Tickets")]
    public class Ticket
    {
        [Key]
        public int TicketId { get; set; }

        [Required, StringLength(100)]
        public string TicketNumber { get; set; }

        // optional human-friendly seat number (e.g. "A12")
        [StringLength(50)]
        public string SeatNumber { get; set; }

        // relates back to booking
        public int? BookingId { get; set; }
        [ForeignKey("BookingId")]
        public virtual Booking Booking { get; set; }

        public int TicketTypeId { get; set; }
        [ForeignKey("TicketTypeId")]
        public virtual TicketType TicketType { get; set; }

        public DateTime IssueDate { get; set; }

        // QR code stored as data URI or raw base64; keep both names if code references either
        public string QrCodeBase64 { get; set; }

        // convenience property expected by some views/services
        public string QrCode { get; set; }
    }
}