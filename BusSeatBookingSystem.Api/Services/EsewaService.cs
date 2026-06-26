using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BusSeatBookingSystem.Repository.Data;
using BusSeatBookingSystem.Service.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using static BusSeatBookingSystem.Entity.Entities.enums;

namespace BusSeatBookingSystem.Api.Services;

public interface IEsewaService
{
    Task<OperationResult> CreatePaymentAsync(string userId, Guid bookingId, CancellationToken ct);
    Task<OperationResult> VerifyAsync(string encodedData, CancellationToken ct);
}

public class EsewaService(ApplicationDbContext db, IHttpClientFactory clients, IOptions<EsewaOptions> options, ITicketEmailService ticketEmail, ILogger<EsewaService> logger) : IEsewaService
{
    private readonly EsewaOptions _options = options.Value;

    public async Task<OperationResult> CreatePaymentAsync(string userId, Guid bookingId, CancellationToken ct)
    {
        var booking = await db.Bookings.SingleOrDefaultAsync(x => x.BookingId == bookingId && x.UserId == userId, ct);
        if (booking is null) return new(false, "Booking not found.");
        if (booking.Status != BookingStatus.PendingPayment || booking.ReservedUntilUtc <= DateTime.UtcNow)
            return new(false, "Booking is not payable or the reservation has expired.");

        var transactionUuid = $"{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..12]}";
        var amount = booking.TotalAmount.ToString("0.00", CultureInfo.InvariantCulture);
        var signedNames = "total_amount,transaction_uuid,product_code";
        var message = $"total_amount={amount},transaction_uuid={transactionUuid},product_code={_options.ProductCode}";
        var fields = new Dictionary<string, string>
        {
            ["amount"] = amount, ["tax_amount"] = "0", ["total_amount"] = amount,
            ["transaction_uuid"] = transactionUuid, ["product_code"] = _options.ProductCode,
            ["product_service_charge"] = "0", ["product_delivery_charge"] = "0",
            ["success_url"] = _options.SuccessUrl, ["failure_url"] = _options.FailureUrl,
            ["signed_field_names"] = signedNames, ["signature"] = Sign(message)
        };
        db.Payments.Add(new() { BookingId = bookingId, TransactionUuid = transactionUuid, Amount = booking.TotalAmount });
        await db.SaveChangesAsync(ct);
        return new(true, "Payment form created.", new PaymentFormResponse(_options.PaymentUrl, fields, bookingId, booking.ReservedUntilUtc));
    }

    public async Task<OperationResult> VerifyAsync(string encodedData, CancellationToken ct)
    {
        Dictionary<string, JsonElement>? payload;
        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(encodedData));
            payload = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
        }
        catch { return new(false, "Invalid eSewa callback data."); }
        if (payload is null) return new(false, "Empty eSewa callback data.");

        string Read(string key) => payload.TryGetValue(key, out var value) ? value.ToString() : string.Empty;
        var signedNames = Read("signed_field_names").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var signatureMessage = string.Join(',', signedNames.Select(name => $"{name}={Read(name)}"));
        if (!SecureEquals(Sign(signatureMessage), Read("signature"))) return new(false, "Invalid eSewa signature.");
        if (!string.Equals(Read("product_code"), _options.ProductCode, StringComparison.Ordinal) ||
            !string.Equals(Read("status"), "COMPLETE", StringComparison.OrdinalIgnoreCase))
            return new(false, "eSewa did not report a completed payment.");

        var transactionUuid = Read("transaction_uuid");
        var payment = await db.Payments.Include(x => x.Booking).ThenInclude(x => x.BookingSeats).ThenInclude(x => x.Seat)
            .SingleOrDefaultAsync(x => x.TransactionUuid == transactionUuid, ct);
        if (payment is null) return new(false, "Payment transaction was not found.");
        if (payment.Status == PaymentStatus.Complete)
        {
            var existingEmail = await ticketEmail.SendTicketAsync(payment.BookingId, ct);
            return new(true, existingEmail.Success ? "Payment was already verified and the ticket email was sent again." : "Payment was already verified. Email delivery is still pending.", new PaymentVerificationData(payment.BookingId, payment.EsewaReferenceId, existingEmail.Success));
        }

        var amount = payment.Amount.ToString("0.00", CultureInfo.InvariantCulture);
        var statusUrl = $"{_options.StatusUrl}?product_code={Uri.EscapeDataString(_options.ProductCode)}&total_amount={Uri.EscapeDataString(amount)}&transaction_uuid={Uri.EscapeDataString(transactionUuid)}";
        HttpResponseMessage response;
        try
        {
            response = await clients.CreateClient("Esewa").GetAsync(statusUrl, ct);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            logger.LogWarning("eSewa status verification timed out for transaction {TransactionUuid}", transactionUuid);
            return new(false, "eSewa verification timed out. Please try opening your booking again shortly.");
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Could not reach eSewa status API for transaction {TransactionUuid}", transactionUuid);
            return new(false, "eSewa verification is temporarily unavailable. Please try again shortly.");
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("eSewa status API returned {StatusCode} for transaction {TransactionUuid}", response.StatusCode, transactionUuid);
                return new(false, "Could not verify payment with eSewa.");
            }

            JsonDocument statusJson;
            try
            {
                statusJson = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "eSewa returned invalid JSON for transaction {TransactionUuid}", transactionUuid);
                return new(false, "eSewa returned an invalid verification response.");
            }

            using (statusJson)
            {
                var status = statusJson.RootElement.TryGetProperty("status", out var statusValue) ? statusValue.GetString() : null;
                if (!string.Equals(status, "COMPLETE", StringComparison.OrdinalIgnoreCase))
                    return new(false, $"Payment status is {status ?? "UNKNOWN"}.");
            }
        }

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        db.ChangeTracker.Clear();
        payment = await db.Payments.Include(x => x.Booking).ThenInclude(x => x.BookingSeats).ThenInclude(x => x.Seat)
            .SingleAsync(x => x.TransactionUuid == transactionUuid, ct);
        if (payment.Status == PaymentStatus.Complete)
        {
            await transaction.CommitAsync(ct);
            var existingEmail = await ticketEmail.SendTicketAsync(payment.BookingId, ct);
            return new(true, existingEmail.Success ? "Payment was already verified and the ticket email was sent again." : "Payment was already verified. Email delivery is still pending.", new PaymentVerificationData(payment.BookingId, payment.EsewaReferenceId, existingEmail.Success));
        }
        if (payment.Booking.Status != BookingStatus.PendingPayment || payment.Booking.ReservedUntilUtc <= DateTime.UtcNow ||
            payment.Booking.BookingSeats.Any(x => x.Seat.Status != SeatStatus.Reserved))
        {
            payment.Status = PaymentStatus.Failed;
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return new(false, "Payment completed after the seat reservation expired. Manual refund review is required.", new { payment.PaymentId });
        }
        payment.Status = PaymentStatus.Complete;
        payment.EsewaReferenceId = Read("transaction_code");
        payment.VerifiedAtUtc = DateTime.UtcNow;
        payment.Booking.Status = BookingStatus.Confirmed;
        payment.Booking.ConfirmedAtUtc = DateTime.UtcNow;
        foreach (var item in payment.Booking.BookingSeats)
        {
            item.Seat.Status = SeatStatus.Booked;
            item.Seat.ReservedUntilUtc = null;
        }
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        var emailResult = await ticketEmail.SendTicketAsync(payment.BookingId, ct);
        if (!emailResult.Success)
            logger.LogWarning("Payment confirmed but ticket email was not delivered for booking {BookingId}: {Message}", payment.BookingId, emailResult.Message);
        return new(true, emailResult.Success ? "Payment verified, booking confirmed, and ticket emailed." : "Payment verified and booking confirmed. Email delivery is pending.", new PaymentVerificationData(payment.BookingId, payment.EsewaReferenceId, emailResult.Success));
    }

    private string Sign(string message)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.SecretKey));
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(message)));
    }
    private static bool SecureEquals(string left, string right)
    {
        try { return CryptographicOperations.FixedTimeEquals(Convert.FromBase64String(left), Convert.FromBase64String(right)); }
        catch { return false; }
    }
}





