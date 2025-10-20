using System;
using System.Collections.Generic;

namespace StarEvents.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalEvents { get; set; }
        public int ActiveEvents { get; set; }
        public int TotalVenues { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalOrganizers { get; set; }

        public List<RecentBookingViewModel> RecentBookings { get; set; } = new List<RecentBookingViewModel>();
    }

    public class RecentBookingViewModel
    {
        public int BookingId { get; set; }
        public string BookingReference { get; set; }
        public string EventName { get; set; }
        public decimal FinalAmount { get; set; }
        public DateTime BookingDate { get; set; }
    }
}