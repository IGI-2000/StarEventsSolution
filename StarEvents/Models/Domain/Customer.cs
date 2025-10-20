using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarEvents.Models.Domain
{
    [Table("Customers")]
    public class Customer : BaseUser
    {
        private string _address;
        private int _loyaltyPoints;
        private DateTime? _dateOfBirth;

        public Customer()
        {
            Bookings = new HashSet<Booking>();
            Role = UserRole.Customer;
            IsActive = true;
            CreatedDate = DateTime.UtcNow;
        }

        public string Address
        {
            get => _address;
            set => _address = value?.Trim();
        }

        public int LoyaltyPoints
        {
            get => _loyaltyPoints;
            set => _loyaltyPoints = value;
        }

        public DateTime? DateOfBirth
        {
            get => _dateOfBirth;
            set => _dateOfBirth = value;
        }

        public virtual ICollection<Booking> Bookings { get; set; }

        public override bool ValidateUserData()
        {
            if (string.IsNullOrWhiteSpace(Email)) return false;
            if (string.IsNullOrWhiteSpace(FirstName)) return false;
            if (string.IsNullOrWhiteSpace(LastName)) return false;
            return true;
        }
    }
}