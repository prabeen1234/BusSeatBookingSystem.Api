using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BusSeatBookingSystem.Api.Services;
using BusSeatBookingSystem.Entity.Entities.UserEntities;
using BusSeatBookingSystem.Service.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;

namespace BusSeatBookingSystem.Api.Controllers.AuthControllers;

[Route("api/auth"), ApiController, EnableRateLimiting("auth")]
public class AuthController(UserManager<ApplicationUser> users, IConfiguration configuration, IPasswordOtpService passwordOtps) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await users.FindByEmailAsync(email) is not null) return Conflict(new OperationResult(false, "Email is already registered."));
        var user = new ApplicationUser { UserName = email, Email = email, FirstName = request.FirstName.Trim(), LastName = request.LastName.Trim(), Gender = request.Gender };
        var result = await users.CreateAsync(user, request.Password);
        if (!result.Succeeded) return BadRequest(new OperationResult(false, result.Errors.First().Description, result.Errors.Select(x => x.Description).ToArray()));
        await users.AddToRoleAsync(user, "User");
        return Ok(new OperationResult(true, "Registration successful. You can now sign in."));
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await users.FindByEmailAsync(request.Email.Trim());
        if (user is null) return Unauthorized(new OperationResult(false, "Invalid email or password."));
        if (await users.IsLockedOutAsync(user)) return StatusCode(StatusCodes.Status423Locked, new OperationResult(false, "Account temporarily locked after repeated failed sign-in attempts. Try again in 15 minutes."));
        if (!await users.CheckPasswordAsync(user, request.Password))
        {
            await users.AccessFailedAsync(user);
            return Unauthorized(new OperationResult(false, "Invalid email or password."));
        }
        await users.ResetAccessFailedCountAsync(user);

        var roles = await users.GetRolesAsync(user);
        var claims = new List<Claim> { new(JwtRegisteredClaimNames.Sub, user.Id), new(ClaimTypes.NameIdentifier, user.Id), new(ClaimTypes.Email, user.Email!), new(ClaimTypes.Name, user.UserName!), new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), new("security_stamp", user.SecurityStamp ?? string.Empty) };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!));
        var expires = DateTime.UtcNow.AddHours(4);
        var token = new JwtSecurityToken(configuration["Jwt:Issuer"], configuration["Jwt:Audience"], claims, expires: expires, signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc = expires, roles });
    }

    [HttpPost("forgot-password/request-otp")]
    public async Task<IActionResult> RequestForgotOtp(ForgotPasswordRequest request, CancellationToken ct)
    {
        var result = await passwordOtps.RequestForgotAsync(request.Email, ct);
        return Ok(result);
    }

    [HttpPost("forgot-password/reset")]
    public async Task<IActionResult> ResetForgotPassword(ResetPasswordWithOtpRequest request, CancellationToken ct)
    {
        var result = await passwordOtps.ResetForgotAsync(request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("change-password/request-otp"), Authorize]
    public async Task<IActionResult> RequestChangeOtp(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await passwordOtps.RequestChangeAsync(userId, ct);
        return result.Success ? Ok(result) : StatusCode(StatusCodes.Status503ServiceUnavailable, result);
    }

    [HttpPost("change-password/confirm"), Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordOtpRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await passwordOtps.ChangeAsync(userId, request, ct);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
