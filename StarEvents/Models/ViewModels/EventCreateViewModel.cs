using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using StarEvents.Models.Domain;

namespace StarEvents.Models.ViewModels
{
    public class EventCreateViewModel
    {
        public int EventId { get; set; }

        [Required, StringLength(200)]
        public string EventName { get; set; }

        [Required, StringLength(2000)]
        public string Description { get; set; }

        [Required]
        public DateTime? EventDate { get; set; }

        public DateTime? EventEndDate { get; set; }

        public string Category { get; set; }

        public string ImageUrl { get; set; }

        [Required]
        public int TotalSeats { get; set; }

        public bool IsActive { get; set; }

        public bool IsPublished { get; set; }

        public DateTime? PublishedDate { get; set; }

        // Make VenueId nullable so controller can use HasValue / Value
        public int? VenueId { get; set; }

        // SelectList items for dropdown (populated by controller)
        public List<SelectListItem> AvailableVenues { get; set; } = new List<SelectListItem>();

        // Inline create-new-venue support
        public bool UseNewVenue { get; set; }
        [StringLength(200)]
        public string NewVenueName { get; set; }
        [StringLength(500)]
        public string NewVenueAddress { get; set; }

        // Ticket types and discounts
        public List<TicketTypeCreateViewModel> TicketTypes { get; set; } = new List<TicketTypeCreateViewModel>();
        public List<DiscountCreateViewModel> Discounts { get; set; } = new List<DiscountCreateViewModel>();
    }

    public class TicketTypeCreateViewModel
    {
        [Required]
        public string TypeName { get; set; }
        [Required]
        public decimal Price { get; set; }
        [Required]
        public int AvailableQuantity { get; set; }
        public string Description { get; set; }
    }

    public class DiscountCreateViewModel
    {
        [Required]
        public string DiscountCode { get; set; }
        public string Description { get; set; }
        [Required]
        public decimal DiscountPercentage { get; set; }
        public decimal? MaxDiscountAmount { get; set; }
        [Required]
        public DateTime ValidFrom { get; set; }
        [Required]
        public DateTime ValidTo { get; set; }
        public bool IsActive { get; set; } = true;
    }
}