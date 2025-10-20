using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace StarEvents.Models.ViewModels
{
    public class UserViewModel
    {
        public int Id { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string Role { get; set; }
        public bool IsActive { get; set; }
    }
}