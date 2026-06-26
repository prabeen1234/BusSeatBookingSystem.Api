namespace BusSeatBookingSystem.Entity.Entities.BusEntities;

public class BookingSeat
{
    public Guid BookingId { get; set; }
    public Booking Booking { get; set; } = null!;
    public Guid SeatId { get; set; }
    public BusSeat Seat { get; set; } = null!;
    public decimal Fare { get; set; }
}
