using System;

namespace StarEvents.Models.ViewModels
{
    public class EventAdminViewModel
    {
        public int EventId { get; set; }
        public string EventName { get; set; }
        public DateTime? EventDate { get; set; }
        public string VenueName { get; set; }
        public int TotalTicketTypes { get; set; }
        public int TotalBookings { get; set; }
    }
}