using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarEvents.Models.Domain
{
    [Table("Admins")]
    public class Admin : BaseUser
    {
        private string _adminLevel;
        private DateTime? _lastPasswordChangeDate;

        public Admin()
        {
            Role = UserRole.Admin;
            IsActive = true;
            CreatedDate = DateTime.UtcNow;
        }

        public string AdminLevel
        {
            get => _adminLevel;
            set => _adminLevel = value?.Trim();
        }

        public DateTime? LastPasswordChangeDate
        {
            get => _lastPasswordChangeDate;
            set => _lastPasswordChangeDate = value;
        }

        public override bool ValidateUserData()
        {
            return !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(AdminLevel);
        }
    }

    [Table("EventOrganizers")]
    public class EventOrganizer : BaseUser
    {
        private string _companyName;
        private string _businessRegistrationNumber;
        private string _bankAccountDetails;
        private bool _isVerified;

        public EventOrganizer()
        {
            Events = new HashSet<Event>();
            Role = UserRole.Organizer;
            IsActive = true;
            CreatedDate = DateTime.UtcNow;
        }

        public string CompanyName
        {
            get => _companyName;
            set => _companyName = value?.Trim();
        }

        public string BusinessRegistrationNumber
        {
            get => _businessRegistrationNumber;
            set => _businessRegistrationNumber = value?.Trim();
        }

        public string BankAccountDetails
        {
            get => _bankAccountDetails;
            set => _bankAccountDetails = value?.Trim();
        }

        public bool IsVerified
        {
            get => _isVerified;
            set => _isVerified = value;
        }

        public virtual ICollection<Event> Events { get; set; }

        public override bool ValidateUserData()
        {
            return !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(CompanyName);
        }
    }
}