using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StarEvents.Models.Domain
{
    public class TicketType
    {
        private int _ticketTypeId;
        private string _typeName;
        private decimal _price;
        private int _availableQuantity;
        private string _description;
        private int _eventId;

        [Key]
        public int TicketTypeId
        {
            get => _ticketTypeId;
            set => _ticketTypeId = value;
        }

        [StringLength(100)]
        public string TypeName
        {
            get => _typeName;
            set => _typeName = value?.Trim();
        }

        [Column(TypeName = "decimal")]
        public decimal Price
        {
            get => _price;
            set => _price = value;
        }

        public int AvailableQuantity
        {
            get => _availableQuantity;
            set => _availableQuantity = value;
        }

        [StringLength(1000)]
        public string Description
        {
            get => _description;
            set => _description = value?.Trim();
        }

        public int EventId
        {
            get => _eventId;
            set => _eventId = value;
        }

        public virtual Event Event { get; set; }
        public virtual ICollection<BookingDetail> BookingDetails { get; set; } = new HashSet<BookingDetail>();
    }
}