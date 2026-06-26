using BusSeatBookingSystem.Repository.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BusSeatBookingSystem.Api.Controllers.AdminControllers;

[ApiController, Route("api/admin/bookings"), Authorize(Roles = "Admin")]
public class BookingsController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) => Ok(await db.Bookings.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc)
        .Select(x => new { x.BookingId, x.BookingNumber, x.Status, x.TotalAmount, x.CreatedAtUtc, x.ConfirmedAtUtc, Passenger = x.PassengerName, Email = x.PassengerEmail, Phone = x.PassengerPhone, x.Bus.BusName, x.Bus.BusNumber, Route = x.Bus.Route.Source + " - " + x.Bus.Route.Destination, Seats = x.BookingSeats.Select(s => s.Seat.SeatNumber) }).ToListAsync(ct));
}

