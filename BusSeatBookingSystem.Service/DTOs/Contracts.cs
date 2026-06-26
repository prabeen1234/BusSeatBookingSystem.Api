using System.ComponentModel.DataAnnotations;
using static BusSeatBookingSystem.Entity.Entities.enums;

namespace BusSeatBookingSystem.Service.DTOs;

public class RegisterRequest
{
    [Required] public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, MinLength(8)] public string Password { get; set; } = string.Empty;
    public Gender Gender { get; set; }
}
public class LoginRequest
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}
public class ForgotPasswordRequest
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
}
public class ResetPasswordWithOtpRequest
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required, RegularExpression("^[0-9]{6}$")] public string Otp { get; set; } = string.Empty;
    [Required, MinLength(8)] public string NewPassword { get; set; } = string.Empty;
}
public class ChangePasswordOtpRequest
{
    [Required] public string CurrentPassword { get; set; } = string.Empty;
    [Required, RegularExpression("^[0-9]{6}$")] public string Otp { get; set; } = string.Empty;
    [Required, MinLength(8)] public string NewPassword { get; set; } = string.Empty;
}
public class RouteRequest
{
    [Required, MaxLength(100)] public string Source { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string Destination { get; set; } = string.Empty;
    [MaxLength(500)] public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}
public class BusRequest
{
    [Required] public string BusName { get; set; } = string.Empty;
    [Required] public string BusNumber { get; set; } = string.Empty;
    [Required] public string BusType { get; set; } = string.Empty;
    public Guid RouteId { get; set; }
    [Range(1, 100)] public int TotalSeats { get; set; }
    [Range(0.01, double.MaxValue)] public decimal Fare { get; set; }
    public DateTime DepartureDateTime { get; set; }
    public DateTime ArrivalDateTime { get; set; }
    public bool IsActive { get; set; } = true;
}
public class BookSeatsRequest
{
    public Guid BusId { get; set; }
    [Required, MinLength(1)] public List<Guid> SeatIds { get; set; } = new();
    [Required, MaxLength(120)] public string PassengerName { get; set; } = string.Empty;
    [Required, EmailAddress, MaxLength(256)] public string PassengerEmail { get; set; } = string.Empty;
    [Required, RegularExpression("^[0-9+() -]{7,20}$"), MaxLength(20)] public string PassengerPhone { get; set; } = string.Empty;
}
public record PaymentFormResponse(string PaymentUrl, Dictionary<string, string> Fields, Guid BookingId, DateTime ReservedUntilUtc);
public record OperationResult(bool Success, string Message, object? Data = null);
public class EsewaOptions
{
    public string PaymentUrl { get; set; } = string.Empty;
    public string StatusUrl { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string SuccessUrl { get; set; } = string.Empty;
    public string FailureUrl { get; set; } = string.Empty;
}
public class ImageStorageOptions
{
    public string RootPath { get; set; } = string.Empty;
    public long MaxFileSizeBytes { get; set; } = 5_242_880;
}


