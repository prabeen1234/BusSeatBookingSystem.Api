using BusSeatBookingSystem.Entity.Entities.BusEntities;
using BusSeatBookingSystem.Repository.Data;
using BusSeatBookingSystem.Service.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BusSeatBookingSystem.Api.Controllers.AdminControllers;

[ApiController, Route("api/admin/routes"), Authorize(Roles = "Admin")]
public class RoutesController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) => Ok(await db.BusRoutes.AsNoTracking().OrderBy(x => x.Source).ToListAsync(ct));

    [HttpPost]
    public async Task<IActionResult> Create(RouteRequest request, CancellationToken ct)
    {
        var source = request.Source.Trim();
        var destination = request.Destination.Trim();
        if (source.Length == 0 || destination.Length == 0) return BadRequest("Source and destination are required.");
        if (source.Equals(destination, StringComparison.OrdinalIgnoreCase)) return BadRequest("Source and destination must differ.");
        if (await db.BusRoutes.AnyAsync(x => x.Source == source && x.Destination == destination, ct))
            return Conflict("This route already exists.");

        var route = new BusRoute
        {
            Source = source,
            Destination = destination,
            Description = request.Description?.Trim(),
            IsActive = request.IsActive
        };
        db.BusRoutes.Add(route);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = route.RouteId }, route);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct) =>
        await db.BusRoutes.AsNoTracking().SingleOrDefaultAsync(x => x.RouteId == id, ct) is { } route ? Ok(route) : NotFound();

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, RouteRequest request, CancellationToken ct)
    {
        var route = await db.BusRoutes.FindAsync([id], ct);
        if (route is null) return NotFound();

        var source = request.Source.Trim();
        var destination = request.Destination.Trim();
        if (source.Length == 0 || destination.Length == 0) return BadRequest("Source and destination are required.");
        if (source.Equals(destination, StringComparison.OrdinalIgnoreCase)) return BadRequest("Source and destination must differ.");
        if (await db.BusRoutes.AnyAsync(x => x.RouteId != id && x.Source == source && x.Destination == destination, ct))
            return Conflict("This route already exists.");

        route.Source = source;
        route.Destination = destination;
        route.Description = request.Description?.Trim();
        route.IsActive = request.IsActive;
        await db.SaveChangesAsync(ct);
        return Ok(route);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var route = await db.BusRoutes.Include(x => x.Buses).SingleOrDefaultAsync(x => x.RouteId == id, ct);
        if (route is null) return NotFound();
        if (route.Buses.Count != 0) return Conflict("Delete or move buses assigned to this route first.");
        db.BusRoutes.Remove(route);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
