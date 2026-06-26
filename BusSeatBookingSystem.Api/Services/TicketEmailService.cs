using System.Net;
using System.Net.Mail;
using System.Text;
using BusSeatBookingSystem.Repository.Data;
using BusSeatBookingSystem.Service.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using static BusSeatBookingSystem.Entity.Entities.enums;

namespace BusSeatBookingSystem.Api.Services;

public interface ITicketEmailService
{
    Task<EmailDeliveryResult> SendTicketAsync(Guid bookingId, CancellationToken ct);
    Task<EmailDeliveryResult> SendPasswordOtpAsync(string recipient, string name, string otp, string purpose, CancellationToken ct);
}

public class TicketEmailService(ApplicationDbContext db, IOptions<EmailOptions> options, ILogger<TicketEmailService> logger) : ITicketEmailService
{
    private readonly EmailOptions _options = options.Value;

    public async Task<EmailDeliveryResult> SendTicketAsync(Guid bookingId, CancellationToken ct)
    {
        var authenticatedSender = string.IsNullOrWhiteSpace(_options.Username) ? _options.SenderEmail.Trim() : _options.Username.Trim();
        if (string.IsNullOrWhiteSpace(authenticatedSender) || string.IsNullOrWhiteSpace(_options.AppPassword))
        {
            logger.LogWarning("Ticket email skipped because SMTP credentials are not configured.");
            return new(false, "Email delivery is not configured. Your ticket is still available in My Bookings.");
        }

        var ticket = await db.Bookings.AsNoTracking().Where(x => x.BookingId == bookingId && x.Status == BookingStatus.Confirmed)
            .Select(x => new
            {
                x.BookingNumber, x.TotalAmount,
                Email = x.PassengerEmail, Name = x.PassengerName,
                x.Bus.BusName, x.Bus.BusNumber, x.Bus.BusType, x.Bus.DepartureDateTime,
                x.Bus.Route.Source, x.Bus.Route.Destination,
                Seats = x.BookingSeats.OrderBy(s => s.Seat.SeatNumber).Select(s => s.Seat.SeatNumber).ToList(),
                Reference = x.Payments.Where(p => p.Status == PaymentStatus.Complete).Select(p => p.EsewaReferenceId).FirstOrDefault()
            }).SingleOrDefaultAsync(ct);

        if (ticket is null) return new(false, "Confirmed ticket was not found.");
        if (string.IsNullOrWhiteSpace(ticket.Email)) return new(false, "This booking does not have a contact email address.");

        var html = $"""
            <div style="font-family:Arial,sans-serif;max-width:680px;margin:auto;border:1px solid #dfe7e2;border-radius:18px;overflow:hidden;color:#18352a">
              <div style="background:#12372a;color:white;padding:28px"><h1 style="margin:0">Your YatraBus ticket</h1><p style="margin-bottom:0">Booking {WebUtility.HtmlEncode(ticket.BookingNumber)}</p></div>
              <div style="padding:28px"><p>Hello {WebUtility.HtmlEncode(ticket.Name.Trim())},</p>
              <p>Your payment is confirmed and your seats are booked.</p>
              <h2 style="color:#12372a">{WebUtility.HtmlEncode(ticket.Source)} &rarr; {WebUtility.HtmlEncode(ticket.Destination)}</h2>
              <table style="width:100%;border-collapse:collapse"><tr><td><b>Bus</b><br>{WebUtility.HtmlEncode(ticket.BusName)} ({WebUtility.HtmlEncode(ticket.BusNumber)})</td><td><b>Type</b><br>{WebUtility.HtmlEncode(ticket.BusType)}</td></tr>
              <tr><td style="padding-top:18px"><b>Departure</b><br>{ticket.DepartureDateTime:ddd, dd MMM yyyy hh:mm tt} UTC</td><td style="padding-top:18px"><b>Seats</b><br>{string.Join(", ", ticket.Seats)}</td></tr></table>
              <div style="margin-top:24px;padding:18px;background:#f3f7f5;border-radius:12px"><b>Paid: NPR {ticket.TotalAmount:0.00}</b><br>eSewa reference: {WebUtility.HtmlEncode(ticket.Reference ?? "N/A")}</div>
              <p style="margin-top:24px">Please arrive at least 20 minutes before departure and carry a valid ID.</p></div>
            </div>
            """;

        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(authenticatedSender, _options.SenderName),
                Subject = $"Confirmed YatraBus ticket {ticket.BookingNumber}",
                Body = html,
                IsBodyHtml = true,
                BodyEncoding = Encoding.UTF8,
                SubjectEncoding = Encoding.UTF8
            };
            message.To.Add(ticket.Email.Trim());

            using var smtp = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = _options.EnableSsl,
                UseDefaultCredentials = false,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Credentials = new NetworkCredential(authenticatedSender, _options.AppPassword)
            };
            await smtp.SendMailAsync(message, ct);
            logger.LogInformation("Ticket email sent for booking {BookingId} to {Recipient}", bookingId, ticket.Email);
            return new(true, $"Ticket sent to {MaskEmail(ticket.Email)}.", ticket.Email);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning("Ticket email timed out for booking {BookingId}", bookingId);
            return new(false, "Email delivery timed out. Please try Email ticket again.", ticket.Email);
        }
        catch (SmtpException ex)
        {
            logger.LogError(ex, "SMTP delivery failed for booking {BookingId}", bookingId);
            return new(false, "Email could not be delivered. Verify the Gmail app password and sender account, then try again.", ticket.Email);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected ticket email failure for booking {BookingId}", bookingId);
            return new(false, "Email could not be delivered right now. Your ticket remains available in My Bookings.", ticket.Email);
        }
    }

    public async Task<EmailDeliveryResult> SendPasswordOtpAsync(string recipient, string name, string otp, string purpose, CancellationToken ct)
    {
        var authenticatedSender = string.IsNullOrWhiteSpace(_options.Username) ? _options.SenderEmail.Trim() : _options.Username.Trim();
        if (string.IsNullOrWhiteSpace(authenticatedSender) || string.IsNullOrWhiteSpace(_options.AppPassword))
            return new(false, "Email delivery is not configured.", recipient);

        var safeName = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(name) ? "traveler" : name.Trim());
        var safePurpose = WebUtility.HtmlEncode(purpose);
        var html = $"""
            <div style="font-family:Arial,sans-serif;max-width:600px;margin:auto;border:1px solid #dfe7e2;border-radius:18px;overflow:hidden;color:#18352a">
              <div style="background:#12372a;color:white;padding:26px"><h1 style="margin:0">YatraBus security code</h1></div>
              <div style="padding:28px"><p>Hello {safeName},</p><p>Use this one-time code to {safePurpose}:</p>
              <div style="font-size:36px;letter-spacing:10px;font-weight:800;text-align:center;padding:20px;background:#f3f7f5;border-radius:12px;color:#12372a">{otp}</div>
              <p>This code expires in 10 minutes. Never share it with anyone.</p><p>If you did not request this, you can safely ignore this email.</p></div>
            </div>
            """;
        return await SendAsync(recipient, "Your YatraBus security code", html, ct);
    }

    private async Task<EmailDeliveryResult> SendAsync(string recipient, string subject, string html, CancellationToken ct)
    {
        var authenticatedSender = string.IsNullOrWhiteSpace(_options.Username) ? _options.SenderEmail.Trim() : _options.Username.Trim();
        try
        {
            using var message = new MailMessage
            {
                From = new MailAddress(authenticatedSender, _options.SenderName), Subject = subject, Body = html,
                IsBodyHtml = true, BodyEncoding = Encoding.UTF8, SubjectEncoding = Encoding.UTF8
            };
            message.To.Add(recipient.Trim());
            using var smtp = new SmtpClient(_options.Host, _options.Port)
            {
                EnableSsl = _options.EnableSsl, UseDefaultCredentials = false,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Credentials = new NetworkCredential(authenticatedSender, _options.AppPassword)
            };
            await smtp.SendMailAsync(message, ct);
            logger.LogInformation("Security email sent to {Recipient}", recipient);
            return new(true, $"Security code sent to {MaskEmail(recipient)}.", recipient);
        }
        catch (Exception ex) when (ex is SmtpException or InvalidOperationException)
        {
            logger.LogError(ex, "Security email delivery failed for {Recipient}", recipient);
            return new(false, "Security code email could not be delivered. Please try again later.", recipient);
        }
    }
    private static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 1) return email;
        return $"{email[0]}***{email[(at - 1)..]}";
    }
}



