using BusSeatBookingSystem.Api.Services;
using BusSeatBookingSystem.Entity.Entities.BusEntities;
using BusSeatBookingSystem.Repository.Data;
using BusSeatBookingSystem.Service.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BusSeatBookingSystem.Api.Controllers.AdminControllers;

[ApiController, Route("api/admin/buses"), Authorize(Roles = "Admin")]
public class BusesController(ApplicationDbContext db, IImageStorageService images) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) => Ok(await db.BusDetails.AsNoTracking().Include(x => x.Route)
        .Select(x => new { x.BusId, x.BusName, x.BusNumber, x.BusType, x.TotalSeats, x.Fare, x.DepartureDateTime, x.ArrivalDateTime, x.IsActive, Route = new { x.Route.RouteId, x.Route.Source, x.Route.Destination }, ImageUrl = x.ImagePath == null ? null : $"/images/{x.ImagePath}" }).ToListAsync(ct));

    [HttpPost, Consumes("multipart/form-data")]
    public async Task<IActionResult> Create([FromForm] BusRequest request, IFormFile image, CancellationToken ct)
    {
        if (request.ArrivalDateTime <= request.DepartureDateTime) return BadRequest("Arrival must be after departure.");
        if (!await db.BusRoutes.AnyAsync(x => x.RouteId == request.RouteId && x.IsActive, ct)) return BadRequest("Active route not found.");
        string? imagePath = null;
        try
        {
            imagePath = await images.SaveAsync(image, ct);
            var bus = new BusDetails { BusName = request.BusName.Trim(), BusNumber = request.BusNumber.Trim(), BusType = request.BusType.Trim(), RouteId = request.RouteId, TotalSeats = request.TotalSeats, Fare = request.Fare, DepartureDateTime = request.DepartureDateTime.ToUniversalTime(), ArrivalDateTime = request.ArrivalDateTime.ToUniversalTime(), IsActive = request.IsActive, ImagePath = imagePath };
            for (var i = 1; i <= bus.TotalSeats; i++) bus.Seats.Add(new BusSeat { SeatNumber = $"S{i:00}" });
            db.BusDetails.Add(bus); await db.SaveChangesAsync(ct);
            return CreatedAtAction(nameof(GetById), new { id = bus.BusId }, new { bus.BusId, bus.BusName, ImageUrl = $"/images/{imagePath}" });
        }
        catch (InvalidOperationException ex) { if (imagePath is not null) images.Delete(imagePath); return BadRequest(ex.Message); }
        catch { if (imagePath is not null) images.Delete(imagePath); throw; }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var bus = await db.BusDetails.AsNoTracking().Include(x => x.Route).Include(x => x.Seats).SingleOrDefaultAsync(x => x.BusId == id, ct);
        return bus is null ? NotFound() : Ok(new { bus.BusId, bus.BusName, bus.BusNumber, bus.BusType, bus.TotalSeats, bus.Fare, bus.DepartureDateTime, bus.ArrivalDateTime, bus.IsActive, bus.Route, ImageUrl = bus.ImagePath == null ? null : $"/images/{bus.ImagePath}", Seats = bus.Seats.OrderBy(x => x.SeatNumber) });
    }

    [HttpPut("{id:guid}"), Consumes("multipart/form-data")]
    public async Task<IActionResult> Update(Guid id, [FromForm] BusRequest request, IFormFile? image, CancellationToken ct)
    {
        var bus = await db.BusDetails.Include(x => x.Seats).Include(x => x.Bookings).SingleOrDefaultAsync(x => x.BusId == id, ct);
        if (bus is null) return NotFound();
        if (!await db.BusRoutes.AnyAsync(x => x.RouteId == request.RouteId, ct)) return BadRequest("Route not found.");
        if (request.TotalSeats != bus.TotalSeats && bus.Bookings.Count != 0) return Conflict("Seat count cannot change after bookings exist.");
        string? newImage = null;
        try
        {
            if (image is not null) newImage = await images.SaveAsync(image, ct);
            if (request.TotalSeats != bus.TotalSeats)
            {
                db.BusSeats.RemoveRange(bus.Seats);
                bus.Seats = Enumerable.Range(1, request.TotalSeats).Select(i => new BusSeat { SeatNumber = $"S{i:00}" }).ToList();
            }
            var oldImage = bus.ImagePath;
            bus.BusName = request.BusName.Trim(); bus.BusNumber = request.BusNumber.Trim(); bus.BusType = request.BusType.Trim(); bus.RouteId = request.RouteId; bus.TotalSeats = request.TotalSeats; bus.Fare = request.Fare; bus.DepartureDateTime = request.DepartureDateTime.ToUniversalTime(); bus.ArrivalDateTime = request.ArrivalDateTime.ToUniversalTime(); bus.IsActive = request.IsActive;
            if (newImage is not null) bus.ImagePath = newImage;
            await db.SaveChangesAsync(ct);
            if (newImage is not null) images.Delete(oldImage);
            return Ok(new { bus.BusId, ImageUrl = bus.ImagePath == null ? null : $"/images/{bus.ImagePath}" });
        }
        catch (InvalidOperationException ex) { if (newImage is not null) images.Delete(newImage); return BadRequest(ex.Message); }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var bus = await db.BusDetails.Include(x => x.Bookings).Include(x => x.Seats).SingleOrDefaultAsync(x => x.BusId == id, ct);
        if (bus is null) return NotFound();
        if (bus.Bookings.Count != 0) return Conflict("A bus with booking history cannot be deleted; mark it inactive instead.");
        var image = bus.ImagePath; db.BusDetails.Remove(bus); await db.SaveChangesAsync(ct); images.Delete(image); return NoContent();
    }
}
