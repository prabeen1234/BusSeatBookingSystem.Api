using System.Security.Claims;
using BusSeatBookingSystem.Api.Services;
using BusSeatBookingSystem.Repository.Data;
using BusSeatBookingSystem.Service.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static BusSeatBookingSystem.Entity.Entities.enums;

namespace BusSeatBookingSystem.Api.Controllers.UserControllers;

[ApiController, Route("api/user/tickets"), Authorize(Roles = "User")]
public class TicketsController(ApplicationDbContext db, ITicketEmailService email) : ControllerBase
{
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("{bookingId:guid}")]
    public async Task<IActionResult> Get(Guid bookingId, CancellationToken ct)
    {
        var ticket = await db.Bookings.AsNoTracking().Where(x => x.BookingId == bookingId && x.UserId == UserId && x.Status == BookingStatus.Confirmed)
            .Select(x => new { x.BookingId, x.BookingNumber, x.TotalAmount, x.ConfirmedAtUtc, x.PassengerName, Email = x.PassengerEmail, Phone = x.PassengerPhone, x.Bus.BusName, x.Bus.BusNumber, x.Bus.BusType, x.Bus.DepartureDateTime, x.Bus.ArrivalDateTime, x.Bus.Route.Source, x.Bus.Route.Destination, ImageUrl = x.Bus.ImagePath == null ? null : $"/images/{x.Bus.ImagePath}", Seats = x.BookingSeats.OrderBy(s => s.Seat.SeatNumber).Select(s => s.Seat.SeatNumber), EsewaReferenceId = x.Payments.Where(p => p.Status == PaymentStatus.Complete).Select(p => p.EsewaReferenceId).FirstOrDefault() }).SingleOrDefaultAsync(ct);
        return ticket is null ? NotFound(new OperationResult(false, "Confirmed ticket not found.")) : Ok(ticket);
    }

    [HttpPost("{bookingId:guid}/email")]
    public async Task<IActionResult> Email(Guid bookingId, CancellationToken ct)
    {
        var exists = await db.Bookings.AnyAsync(x => x.BookingId == bookingId && x.UserId == UserId && x.Status == BookingStatus.Confirmed, ct);
        if (!exists) return NotFound(new OperationResult(false, "Confirmed ticket not found."));
        var result = await email.SendTicketAsync(bookingId, ct);
        return result.Success
            ? Ok(new OperationResult(true, result.Message, new { result.Recipient }))
            : StatusCode(StatusCodes.Status503ServiceUnavailable, new OperationResult(false, result.Message));
    }
}

