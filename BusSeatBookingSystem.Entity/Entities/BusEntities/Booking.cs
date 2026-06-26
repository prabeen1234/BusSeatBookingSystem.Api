using System.ComponentModel.DataAnnotations;
using BusSeatBookingSystem.Entity.Entities.UserEntities;
using static BusSeatBookingSystem.Entity.Entities.enums;

namespace BusSeatBookingSystem.Entity.Entities.BusEntities;

public class Booking
{
    [Key] public Guid BookingId { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public ApplicationUser User { get; set; } = null!;
    public Guid BusId { get; set; }
    public BusDetails Bus { get; set; } = null!;
    [MaxLength(40)] public string BookingNumber { get; set; } = string.Empty;
    [Required, MaxLength(120)] public string PassengerName { get; set; } = string.Empty;
    [Required, MaxLength(256), EmailAddress] public string PassengerEmail { get; set; } = string.Empty;
    [Required, MaxLength(20)] public string PassengerPhone { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.PendingPayment;
    public DateTime ReservedUntilUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ConfirmedAtUtc { get; set; }
    public ICollection<BookingSeat> BookingSeats { get; set; } = new List<BookingSeat>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

