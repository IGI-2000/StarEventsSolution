using System;

namespace StarEvents.Models.ViewModels
{
    public class DiscountViewModel
    {
        public int DiscountId { get; set; }
        public string Code { get; set; }
        public string Description { get; set; }
        public decimal? Percentage { get; set; }
        public decimal? Amount { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; }
        public int? UsageLimit { get; set; }
        public int TimesUsed { get; set; }
    }
}