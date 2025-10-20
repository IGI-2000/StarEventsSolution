using System;
using System.Collections.Generic;

namespace StarEvents.Models.ViewModels
{
    public class BookingViewModel
    {
        public int EventId { get; set; }
        public string EventName { get; set; }
        public DateTime EventDate { get; set; }
        public string VenueName { get; set; }

        public List<TicketSelectionViewModel> TicketSelections { get; set; } = new List<TicketSelectionViewModel>();

        public string DiscountCode { get; set; }
        public int LoyaltyPointsToUse { get; set; }

        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }
    }
}