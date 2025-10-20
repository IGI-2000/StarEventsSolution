using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarEvents.Models.Domain
{
    public abstract class BaseUser
    {
        private int _id;
        private string _email;
        private string _passwordHash;
        private string _firstName;
        private string _lastName;
        private string _phoneNumber;
        private DateTime _createdDate;
        private DateTime? _lastLoginDate;
        private bool _isActive;
        private UserRole _role;

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id
        {
            get => _id;
            set => _id = value;
        }

        [Required, EmailAddress, StringLength(200)]
        public string Email
        {
            get => _email;
            set => _email = value?.Trim();
        }

        [Required, StringLength(200)]
        public string PasswordHash
        {
            get => _passwordHash;
            set => _passwordHash = value;
        }

        [Required, StringLength(100)]
        public string FirstName
        {
            get => _firstName;
            set => _firstName = value?.Trim();
        }

        [Required, StringLength(100)]
        public string LastName
        {
            get => _lastName;
            set => _lastName = value?.Trim();
        }

        [Phone, StringLength(30)]
        public string PhoneNumber
        {
            get => _phoneNumber;
            set => _phoneNumber = value?.Trim();
        }

        public DateTime CreatedDate
        {
            get => _createdDate;
            set => _createdDate = value;
        }

        public DateTime? LastLoginDate
        {
            get => _lastLoginDate;
            set => _lastLoginDate = value;
        }

        public bool IsActive
        {
            get => _isActive;
            set => _isActive = value;
        }

        public UserRole Role
        {
            get => _role;
            set => _role = value;
        }

        // Derived classes implement role-specific validation
        public abstract bool ValidateUserData();
    }
}