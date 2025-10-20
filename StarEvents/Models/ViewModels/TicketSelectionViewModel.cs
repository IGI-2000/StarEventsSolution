using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace StarEvents.Models.ViewModels
{
    public class TicketSelectionViewModel
    {
        public int TicketTypeId { get; set; }
        public string TypeName { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public int AvailableQuantity { get; set; }
    }
}