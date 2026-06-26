using System.ComponentModel.DataAnnotations;

namespace BusSeatBookingSystem.Entity.Entities.BusEntities;

public class BusRoute
{
    [Key] public Guid RouteId { get; set; } = Guid.NewGuid();
    [Required, MaxLength(100)] public string Source { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string Destination { get; set; } = string.Empty;
    [MaxLength(500)] public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<BusDetails> Buses { get; set; } = new List<BusDetails>();
}
