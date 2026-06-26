using System.Security.Claims;
using BusSeatBookingSystem.Api.Services;
using BusSeatBookingSystem.Service.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BusSeatBookingSystem.Api.Controllers.UserControllers;

[ApiController, Route("api/user/payments")]
public class PaymentsController(IEsewaService esewa, IConfiguration configuration) : ControllerBase
{
    [HttpPost("esewa/{bookingId:guid}"), Authorize(Roles = "User")]
    public async Task<IActionResult> Initiate(Guid bookingId, CancellationToken ct)
    {
        var result = await esewa.CreatePaymentAsync(User.FindFirstValue(ClaimTypes.NameIdentifier)!, bookingId, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("esewa/callback"), AllowAnonymous]
    public async Task<IActionResult> Callback([FromQuery] string data, CancellationToken ct)
    {
        var frontend = configuration["Frontend:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:4200";
        var result = await esewa.VerifyAsync(data, ct);
        if (result.Success && result.Data is PaymentVerificationData verified)
            return Redirect($"{frontend}/payment/success?bookingId={verified.BookingId}&emailSent={verified.EmailSent.ToString().ToLowerInvariant()}");
        return Redirect($"{frontend}/payment/failure?message={Uri.EscapeDataString(result.Message)}");
    }

    [HttpGet("esewa/verify"), AllowAnonymous]
    public async Task<IActionResult> Verify([FromQuery] string data, CancellationToken ct)
    {
        var result = await esewa.VerifyAsync(data, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}

