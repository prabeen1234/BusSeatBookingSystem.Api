using System.ComponentModel.DataAnnotations;

namespace BusSeatBookingSystem.Entity.Entities.BusEntities;

public class BusDetails
{
    [Key] public Guid BusId { get; set; } = Guid.NewGuid();
    [Required, MaxLength(120)] public string BusName { get; set; } = string.Empty;
    [Required, MaxLength(50)] public string BusNumber { get; set; } = string.Empty;
    [Required, MaxLength(50)] public string BusType { get; set; } = string.Empty;
    public Guid RouteId { get; set; }
    public BusRoute Route { get; set; } = null!;
    [Range(1, 100)] public int TotalSeats { get; set; }
    [Range(0.01, double.MaxValue)] public decimal Fare { get; set; }
    public DateTime DepartureDateTime { get; set; }
    public DateTime ArrivalDateTime { get; set; }
    [MaxLength(500)] public string? ImagePath { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<BusSeat> Seats { get; set; } = new List<BusSeat>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
