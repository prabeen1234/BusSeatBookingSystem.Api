using System.Data;
using BusSeatBookingSystem.Entity.Entities.BusEntities;
using BusSeatBookingSystem.Repository.Data;
using BusSeatBookingSystem.Service.DTOs;
using Microsoft.EntityFrameworkCore;
using static BusSeatBookingSystem.Entity.Entities.enums;

namespace BusSeatBookingSystem.Service.Services;

public interface IBookingService
{
    Task<OperationResult> ReserveAsync(string userId, BookSeatsRequest request, CancellationToken ct);
    Task<OperationResult> CancelAsync(string userId, Guid bookingId, CancellationToken ct);
}

public class BookingService(ApplicationDbContext db) : IBookingService
{
    private static readonly TimeSpan ReservationPeriod = TimeSpan.FromMinutes(10);

    public async Task<OperationResult> ReserveAsync(string userId, BookSeatsRequest request, CancellationToken ct)
    {
        if (request.SeatIds.Count == 0 || request.SeatIds.Count != request.SeatIds.Distinct().Count())
            return new(false, "Select one or more unique seats.");

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var now = DateTime.UtcNow;
        var bus = await db.BusDetails.SingleOrDefaultAsync(x => x.BusId == request.BusId && x.IsActive, ct);
        if (bus is null || bus.DepartureDateTime <= now)
            return new(false, "Bus was not found or has already departed.");

        var expiredBookings = await db.Bookings
            .Include(x => x.BookingSeats).ThenInclude(x => x.Seat)
            .Where(x => x.BusId == request.BusId && x.Status == BookingStatus.PendingPayment && x.ReservedUntilUtc <= now)
            .ToListAsync(ct);
        foreach (var expired in expiredBookings)
        {
            expired.Status = BookingStatus.Expired;
            foreach (var item in expired.BookingSeats)
            {
                item.Seat.Status = SeatStatus.Available;
                item.Seat.ReservedUntilUtc = null;
            }
        }

        var seats = await db.BusSeats.Where(x => x.BusId == request.BusId && request.SeatIds.Contains(x.SeatId)).ToListAsync(ct);
        if (seats.Count != request.SeatIds.Count)
            return new(false, "One or more seats do not belong to this bus.");
        if (seats.Any(x => x.Status != SeatStatus.Available))
            return new(false, "One or more selected seats are no longer available.");

        var booking = new Booking
        {
            UserId = userId,
            BusId = bus.BusId,
            BookingNumber = $"BK-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
            PassengerName = request.PassengerName.Trim(),
            PassengerEmail = request.PassengerEmail.Trim().ToLowerInvariant(),
            PassengerPhone = request.PassengerPhone.Trim(),
            TotalAmount = bus.Fare * seats.Count,
            ReservedUntilUtc = now.Add(ReservationPeriod)
        };
        foreach (var seat in seats)
        {
            seat.Status = SeatStatus.Reserved;
            seat.ReservedUntilUtc = booking.ReservedUntilUtc;
            booking.BookingSeats.Add(new BookingSeat { Seat = seat, Fare = bus.Fare });
        }
        db.Bookings.Add(booking);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new(true, "Seats reserved for 10 minutes.", new { booking.BookingId, booking.BookingNumber, booking.TotalAmount, booking.ReservedUntilUtc });
    }

    public async Task<OperationResult> CancelAsync(string userId, Guid bookingId, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var booking = await db.Bookings.Include(x => x.BookingSeats).ThenInclude(x => x.Seat)
            .SingleOrDefaultAsync(x => x.BookingId == bookingId && x.UserId == userId, ct);
        if (booking is null) return new(false, "Booking not found.");
        if (booking.Status != BookingStatus.PendingPayment) return new(false, "Only unpaid bookings can be cancelled.");
        booking.Status = BookingStatus.Cancelled;
        foreach (var item in booking.BookingSeats)
        {
            item.Seat.Status = SeatStatus.Available;
            item.Seat.ReservedUntilUtc = null;
        }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new(true, "Booking cancelled.");
    }
}

