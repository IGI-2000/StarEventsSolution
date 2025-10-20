using System;
using System.Collections.Generic;

namespace StarEvents.Models.ViewModels
{
    public class BookingDetailsViewModel
    {
        public int BookingId { get; set; }
        public string BookingReference { get; set; }
        public string EventName { get; set; }
        public DateTime? EventDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }

        // Fully-qualified domain enum to avoid ambiguity
        public global::StarEvents.Models.Domain.BookingStatus Status { get; set; }

        public List<BookingLineItemViewModel> Items { get; set; } = new List<BookingLineItemViewModel>();
    }

    public class BookingLineItemViewModel
    {
        public string TicketTypeName { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Subtotal { get; set; }
    }
}