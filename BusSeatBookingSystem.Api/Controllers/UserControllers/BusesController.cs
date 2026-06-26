using BusSeatBookingSystem.Repository.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static BusSeatBookingSystem.Entity.Entities.enums;

namespace BusSeatBookingSystem.Api.Controllers.UserControllers;

[ApiController, Route("api/user/buses")]
public class BusesController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet("locations")]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any)]
    public async Task<IActionResult> Locations(CancellationToken ct)
    {
        var routes = await db.BusRoutes.AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Source).ThenBy(x => x.Destination)
            .Select(x => new { x.RouteId, x.Source, x.Destination })
            .ToListAsync(ct);

        return Ok(new
        {
            sources = routes.Select(x => x.Source).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x),
            destinations = routes.Select(x => x.Destination).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x),
            routes
        });
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] string? source, [FromQuery] string? destination, [FromQuery] DateTime? departureDate, CancellationToken ct)
    {
        var query = db.BusDetails.AsNoTracking().Where(x => x.IsActive && x.Route.IsActive && x.DepartureDateTime > DateTime.UtcNow);
        if (!string.IsNullOrWhiteSpace(source))
        {
            var value = source.Trim();
            query = query.Where(x => x.Route.Source == value);
        }
        if (!string.IsNullOrWhiteSpace(destination))
        {
            var value = destination.Trim();
            query = query.Where(x => x.Route.Destination == value);
        }
        if (departureDate.HasValue)
        {
            var start = departureDate.Value.Date.ToUniversalTime();
            var end = start.AddDays(1);
            query = query.Where(x => x.DepartureDateTime >= start && x.DepartureDateTime < end);
        }

        return Ok(await query.OrderBy(x => x.DepartureDateTime).Select(x => new
        {
            x.BusId, x.BusName, x.BusNumber, x.BusType, x.Fare, x.TotalSeats,
            x.DepartureDateTime, x.ArrivalDateTime, x.Route.Source, x.Route.Destination,
            ImageUrl = x.ImagePath == null ? null : $"/images/{x.ImagePath}",
            AvailableSeats = x.Seats.Count(s => s.Status == SeatStatus.Available || s.Status == SeatStatus.Reserved && s.ReservedUntilUtc <= DateTime.UtcNow)
        }).ToListAsync(ct));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var bus = await db.BusDetails.AsNoTracking().Include(x => x.Route).Include(x => x.Seats)
            .SingleOrDefaultAsync(x => x.BusId == id && x.IsActive && x.Route.IsActive, ct);
        if (bus is null) return NotFound(new { message = "Bus was not found or is no longer available." });
        return Ok(new
        {
            bus.BusId, bus.BusName, bus.BusNumber, bus.BusType, bus.Fare, bus.TotalSeats,
            bus.DepartureDateTime, bus.ArrivalDateTime, bus.Route.Source, bus.Route.Destination,
            ImageUrl = bus.ImagePath == null ? null : $"/images/{bus.ImagePath}",
            Seats = bus.Seats.OrderBy(x => x.SeatNumber).Select(x => new
            {
                x.SeatId, x.SeatNumber,
                Status = x.Status == SeatStatus.Reserved && x.ReservedUntilUtc <= now ? SeatStatus.Available : x.Status
            })
        });
    }
}
