using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace StarEvents.Models.Domain
{
    public class Event
    {
        private int _eventId;
        private string _eventName;
        private string _description;
        private DateTime _eventDate;
        private DateTime _eventEndDate;
        private string _category;
        private string _imageUrl;
        private int _totalSeats;
        private int _availableSeats;
        private bool _isActive;
        private DateTime _createdDate;
        private int _organizerId;
        private int _venueId;
        private bool _isPublished;
        private DateTime? _publishedDate;

        public Event()
        {
            TicketTypes = new HashSet<TicketType>();
            Bookings = new HashSet<Booking>();
            Discounts = new HashSet<Discount>();
            Tickets = new HashSet<Ticket>();
            CreatedDate = DateTime.UtcNow;
            IsActive = true;
        }

        [Key]
        public int EventId
        {
            get => _eventId;
            set => _eventId = value;
        }

        [Required, StringLength(200)]
        public string EventName
        {
            get => _eventName;
            set => _eventName = value?.Trim();
        }

        [Required, StringLength(2000)]
        public string Description
        {
            get => _description;
            set => _description = value?.Trim();
        }

        [Required]
        public DateTime EventDate
        {
            get => _eventDate;
            set => _eventDate = value;
        }

        public DateTime EventEndDate
        {
            get => _eventEndDate;
            set => _eventEndDate = value;
        }

        [StringLength(100)]
        public string Category
        {
            get => _category;
            set => _category = value?.Trim();
        }

        public string ImageUrl
        {
            get => _imageUrl;
            set => _imageUrl = value?.Trim();
        }

        public int TotalSeats
        {
            get => _totalSeats;
            set => _totalSeats = value;
        }

        public int AvailableSeats
        {
            get => _availableSeats;
            set => _availableSeats = value;
        }

        public bool IsActive
        {
            get => _isActive;
            set => _isActive = value;
        }

        public DateTime CreatedDate
        {
            get => _createdDate;
            set => _createdDate = value;
        }

        // Publishing support
        public bool IsPublished
        {
            get => _isPublished;
            set => _isPublished = value;
        }

        public DateTime? PublishedDate
        {
            get => _publishedDate;
            set => _publishedDate = value;
        }

        // Foreign keys
        public int OrganizerId
        {
            get => _organizerId;
            set => _organizerId = value;
        }

        public int VenueId
        {
            get => _venueId;
            set => _venueId = value;
        }

        // Navigation properties
        public virtual EventOrganizer Organizer { get; set; }
        public virtual Venue Venue { get; set; }

        // Collections
        public virtual ICollection<TicketType> TicketTypes { get; set; }
        public virtual ICollection<Booking> Bookings { get; set; }

        // Added Discounts collection so controllers/views can reference event.Discounts
        public virtual ICollection<Discount> Discounts { get; set; }

        // Tickets generated for this event (if you have a Ticket entity)
        public virtual ICollection<Ticket> Tickets { get; set; }
    }
}