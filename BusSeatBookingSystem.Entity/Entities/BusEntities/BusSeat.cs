using System.ComponentModel.DataAnnotations;
using static BusSeatBookingSystem.Entity.Entities.enums;

namespace BusSeatBookingSystem.Entity.Entities.BusEntities;

public class BusSeat
{
    [Key] public Guid SeatId { get; set; } = Guid.NewGuid();
    public Guid BusId { get; set; }
    public BusDetails Bus { get; set; } = null!;
    [Required, MaxLength(10)] public string SeatNumber { get; set; } = string.Empty;
    public SeatStatus Status { get; set; } = SeatStatus.Available;
    public DateTime? ReservedUntilUtc { get; set; }
    public ICollection<BookingSeat> BookingSeats { get; set; } = new List<BookingSeat>();
}
