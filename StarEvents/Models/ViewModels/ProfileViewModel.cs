using System.ComponentModel.DataAnnotations;

namespace StarEvents.Models.ViewModels
{
    public class ProfileViewModel
    {
        public int UserId { get; set; }

        [Required, EmailAddress]
        public string Email { get; set; }

        [Required, StringLength(100)]
        public string FirstName { get; set; }

        [Required, StringLength(100)]
        public string LastName { get; set; }

        [Phone]
        public string PhoneNumber { get; set; }

        // Optional password change fields
        [DataType(DataType.Password)]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match.")]
        public string ConfirmNewPassword { get; set; }

        // Role specific
        public string Role { get; set; }
        public string Address { get; set; } // for Customer
        public int LoyaltyPoints { get; set; } // for Customer
        public string CompanyName { get; set; } // for Organizer
        public bool IsVerified { get; set; } // for Organizer
        public string AdminLevel { get; set; } // for Admin
    }
}