using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace StarEvents.Models.ViewModels
{
    public class EventEditViewModel
    {
        public int EventId { get; set; }

        [Required, StringLength(200)]
        public string EventName { get; set; }

        [AllowHtml]
        public string Description { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? EventDate { get; set; }

        // Existing venue selection
        public int? VenueId { get; set; }
        public List<SelectListItem> Venues { get; set; } = new List<SelectListItem>();

        // Inline new venue fields (optional)
        [StringLength(200)]
        public string NewVenueName { get; set; }

        [StringLength(500)]
        public string NewVenueAddress { get; set; }

        public bool UseNewVenue => !string.IsNullOrWhiteSpace(NewVenueName);

        // Basic flags
        public bool IsPublished { get; set; }
    }
}