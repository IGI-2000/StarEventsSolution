using System;
using System.Collections.Generic;

namespace StarEvents.Models.ViewModels
{
    public class SystemReportViewModel
    {
        public DateTime GeneratedDate { get; set; } = DateTime.Now;
        public int TotalEvents { get; set; }
        public int TotalCustomers { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal RevenueThisMonth { get; set; }

        public List<CategoryPerformanceViewModel> CategoryPerformance { get; set; } = new List<CategoryPerformanceViewModel>();
    }

    public class CategoryPerformanceViewModel
    {
        public string Category { get; set; }
        public decimal TotalRevenue { get; set; }
        public int EventCount { get; set; }
        public int BookingCount { get; set; }
    }
}