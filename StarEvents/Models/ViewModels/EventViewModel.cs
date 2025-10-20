using System;

namespace StarEvents.Models.ViewModels
{
    public class EventViewModel
    {
        public int EventId { get; set; }

        public string EventName { get; set; }

        public string Description { get; set; }

        // Make nullable to match possible nullable domain fields and avoid implicit conversion errors
        public DateTime? EventDate { get; set; }

        public DateTime? EventEndDate { get; set; }

        public string Category { get; set; }

        public string ImageUrl { get; set; }

        public int TotalSeats { get; set; }

        public int AvailableSeats { get; set; }

        public bool IsActive { get; set; }

        public bool IsPublished { get; set; }

        public DateTime? PublishedDate { get; set; }

        public DateTime CreatedDate { get; set; }

        public int OrganizerId { get; set; }

        public string OrganizerName { get; set; }

        public int VenueId { get; set; }

        public string VenueName { get; set; }
    }
}