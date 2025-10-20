using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace StarEvents.Models.Domain
{
    public class Venue
    {
        private int _venueId;
        private string _venueName;
        private string _address;
        private string _city;
        private string _province;
        private int _capacity;
        private string _contactNumber;
        private string _facilities;

        [Key]
        public int VenueId
        {
            get => _venueId;
            set => _venueId = value;
        }

        [Required, StringLength(200)]
        public string VenueName
        {
            get => _venueName;
            set => _venueName = value?.Trim();
        }

        [Required, StringLength(500)]
        public string Address
        {
            get => _address;
            set => _address = value?.Trim();
        }

        [StringLength(100)]
        public string City
        {
            get => _city;
            set => _city = value?.Trim();
        }

        [StringLength(100)]
        public string Province
        {
            get => _province;
            set => _province = value?.Trim();
        }

        public int Capacity
        {
            get => _capacity;
            set => _capacity = value;
        }

        [StringLength(50)]
        public string ContactNumber
        {
            get => _contactNumber;
            set => _contactNumber = value?.Trim();
        }

        public string Facilities
        {
            get => _facilities;
            set => _facilities = value?.Trim();
        }

        public virtual ICollection<Event> Events { get; set; } = new HashSet<Event>();
    }
}