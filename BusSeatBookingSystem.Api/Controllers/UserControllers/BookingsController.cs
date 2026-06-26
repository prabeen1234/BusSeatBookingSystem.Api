using System.Security.Claims;
using BusSeatBookingSystem.Repository.Data;
using BusSeatBookingSystem.Service.DTOs;
using BusSeatBookingSystem.Service.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BusSeatBookingSystem.Api.Controllers.UserControllers;

[ApiController, Route("api/user/bookings"), Authorize(Roles = "User")]
public class BookingsController(IBookingService bookings, ApplicationDbContext db) : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpPost]
    public async Task<IActionResult> Book(BookSeatsRequest request, CancellationToken ct)
    {
        var result = await bookings.ReserveAsync(UserId, request, ct);
        return result.Success ? Ok(result) : Conflict(result);
    }

    [HttpGet]
    public async Task<IActionResult> Mine(CancellationToken ct) => Ok(await db.Bookings.AsNoTracking().Where(x => x.UserId == UserId)
        .OrderByDescending(x => x.CreatedAtUtc).Select(x => new { x.BookingId, x.BookingNumber, x.Status, x.TotalAmount, x.ReservedUntilUtc, x.CreatedAtUtc, x.ConfirmedAtUtc, x.PassengerName, x.PassengerEmail, x.PassengerPhone, x.Bus.BusName, x.Bus.DepartureDateTime, Seats = x.BookingSeats.Select(s => s.Seat.SeatNumber) }).ToListAsync(ct));

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var result = await bookings.CancelAsync(UserId, id, ct);
        return result.Success ? Ok(result) : Conflict(result);
    }
}

