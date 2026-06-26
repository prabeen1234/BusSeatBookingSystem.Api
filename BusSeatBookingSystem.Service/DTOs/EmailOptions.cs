namespace BusSeatBookingSystem.Service.DTOs;

public class EmailOptions
{
    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string SenderName { get; set; } = "YatraBus";
    public string SenderEmail { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string AppPassword { get; set; } = string.Empty;
}

public record EmailDeliveryResult(bool Success, string Message, string? Recipient = null);
public record PaymentVerificationData(Guid BookingId, string? EsewaReferenceId, bool EmailSent);
