using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static BusSeatBookingSystem.Entity.Entities.enums;

namespace BusSeatBookingSystem.Entity.Entities.UserEntities
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; } = string.Empty;
        public Gender Gender { get; set; } 
    }
}
