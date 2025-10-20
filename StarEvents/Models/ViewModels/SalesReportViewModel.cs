using System;
using System.Collections.Generic;

namespace StarEvents.Models.ViewModels
{
    public class SalesReportViewModel
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalTicketsSold { get; set; }
        public List<SalesReportItemViewModel> Items { get; set; } = new List<SalesReportItemViewModel>();
    }
}