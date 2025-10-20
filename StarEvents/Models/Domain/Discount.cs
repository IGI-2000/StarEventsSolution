using System;
using System.ComponentModel.DataAnnotations;

namespace StarEvents.Models.Domain
{
    public class Discount
    {
        private int _discountId;
        private string _discountCode;
        private string _description;
        private decimal _discountPercentage;
        private decimal? _maxDiscountAmount;
        private DateTime _validFrom;
        private DateTime _validTo;
        private int? _maxUsageCount;
        private int _currentUsageCount;
        private bool _isActive;
        private DiscountType _type;
        private int? _eventId;

        [Key]
        public int DiscountId
        {
            get => _discountId;
            set => _discountId = value;
        }

        [Required, StringLength(50)]
        public string DiscountCode
        {
            get => _discountCode;
            set => _discountCode = value?.Trim();
        }

        [StringLength(1000)]
        public string Description
        {
            get => _description;
            set => _description = value?.Trim();
        }

        public decimal DiscountPercentage
        {
            get => _discountPercentage;
            set => _discountPercentage = value;
        }

        public decimal? MaxDiscountAmount
        {
            get => _maxDiscountAmount;
            set => _maxDiscountAmount = value;
        }

        public DateTime ValidFrom
        {
            get => _validFrom;
            set => _validFrom = value;
        }

        public DateTime ValidTo
        {
            get => _validTo;
            set => _validTo = value;
        }

        public int? MaxUsageCount
        {
            get => _maxUsageCount;
            set => _maxUsageCount = value;
        }

        public int CurrentUsageCount
        {
            get => _currentUsageCount;
            set => _currentUsageCount = value;
        }

        public bool IsActive
        {
            get => _isActive;
            set => _isActive = value;
        }

        public DiscountType Type
        {
            get => _type;
            set => _type = value;
        }

        public int? EventId
        {
            get => _eventId;
            set => _eventId = value;
        }

        public virtual Event Event { get; set; }
    }
}