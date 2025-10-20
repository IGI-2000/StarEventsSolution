using System;
using System.Collections.Generic;

namespace StarEvents.Models.ViewModels
{
    public class EventSearchViewModel
    {
        public string SearchKeyword { get; set; }
        public string Category { get; set; }

        // Single-date convenience (kept for backward compatibility)
        public DateTime? EventDate { get; set; }

        // Range search
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public string Location { get; set; }

        public List<EventViewModel> Results { get; set; } = new List<EventViewModel>();
        public List<string> AvailableCategories { get; set; } = new List<string>();
        public List<string> AvailableLocations { get; set; } = new List<string>();
    }
}