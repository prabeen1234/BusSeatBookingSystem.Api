using Microsoft.AspNetCore.Identity;
using BusSeatBookingSystem.Entity.Entities.BusEntities;
using static BusSeatBookingSystem.Entity.Entities.enums;

namespace BusSeatBookingSystem.Entity.Entities.UserEntities;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public Gender Gender { get; set; }
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<PasswordOtp> PasswordOtps { get; set; } = new List<PasswordOtp>();
}
