using System;

namespace StarEvents.Models.ViewModels
{
    public class TicketViewModel
    {
        public int TicketId { get; set; }
        public string TicketNumber { get; set; }
        public string SeatNumber { get; set; } // New
        public DateTime IssueDate { get; set; }
    }
}