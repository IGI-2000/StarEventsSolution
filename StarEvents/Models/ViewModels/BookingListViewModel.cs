using System;
using System.Collections.Generic;

namespace StarEvents.Models.ViewModels
{
    public class BookingListItemViewModel
    {
        public int BookingId { get; set; }
        public string BookingReference { get; set; }
        public string EventName { get; set; }
        public DateTime BookingDate { get; set; }
        public decimal FinalAmount { get; set; }
        public int TicketCount { get; set; }
        public StarEvents.Models.Domain.BookingStatus Status { get; set; }
        public List<TicketViewModel> Tickets { get; set; } = new List<TicketViewModel>();
    }
}