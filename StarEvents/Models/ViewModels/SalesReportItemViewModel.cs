using System;

namespace StarEvents.Models.ViewModels
{
    public class SalesReportItemViewModel
    {
        public int EventId { get; set; }
        public string EventName { get; set; }
        public DateTime EventDate { get; set; }
        public int TicketsSold { get; set; }
        public decimal Revenue { get; set; }
    }
}