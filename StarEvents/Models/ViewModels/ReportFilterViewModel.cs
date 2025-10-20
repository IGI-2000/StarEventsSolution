using System;

namespace StarEvents.Models.ViewModels
{
    public class ReportFilterViewModel
    {
        public DateTime StartDate { get; set; } = DateTime.Now.AddMonths(-1);
        public DateTime EndDate { get; set; } = DateTime.Now;
        public int? EventId { get; set; }
        public int? OrganizerId { get; set; }
        public string ReportType { get; set; }
    }
}