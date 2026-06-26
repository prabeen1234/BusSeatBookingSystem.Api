using System.Data;
using System.Security.Cryptography;
using System.Text;
using BusSeatBookingSystem.Entity.Entities.UserEntities;
using BusSeatBookingSystem.Repository.Data;
using BusSeatBookingSystem.Service.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using static BusSeatBookingSystem.Entity.Entities.enums;

namespace BusSeatBookingSystem.Api.Services;

public interface IPasswordOtpService
{
    Task<OperationResult> RequestForgotAsync(string email, CancellationToken ct);
    Task<OperationResult> ResetForgotAsync(ResetPasswordWithOtpRequest request, CancellationToken ct);
    Task<OperationResult> RequestChangeAsync(string userId, CancellationToken ct);
    Task<OperationResult> ChangeAsync(string userId, ChangePasswordOtpRequest request, CancellationToken ct);
}

public class PasswordOtpService(ApplicationDbContext db, UserManager<ApplicationUser> users, ITicketEmailService email, ILogger<PasswordOtpService> logger) : IPasswordOtpService
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan ResendCooldown = TimeSpan.FromMinutes(1);
    private const int MaxAttempts = 5;

    public async Task<OperationResult> RequestForgotAsync(string emailAddress, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(emailAddress.Trim());
        if (user is null) return new(true, "If an account exists for that email, a security code has been sent.");
        var delivery = await CreateAndSendAsync(user, PasswordOtpPurpose.ForgotPassword, "reset your password", ct);
        if (!delivery.Success) logger.LogWarning("Forgot-password OTP email was not delivered for user {UserId}: {Message}", user.Id, delivery.Message);
        return new(true, "If an account exists for that email, a security code has been sent.");
    }

    public async Task<OperationResult> RequestChangeAsync(string userId, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(userId);
        if (user?.Email is null) return new(false, "Your account does not have a registered email address.");
        var delivery = await CreateAndSendAsync(user, PasswordOtpPurpose.ChangePassword, "change your password", ct);
        return delivery.Success ? new(true, delivery.Message) : new(false, delivery.Message);
    }

    public async Task<OperationResult> ResetForgotAsync(ResetPasswordWithOtpRequest request, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(request.Email.Trim());
        if (user is null) return InvalidCode();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var otp = await ValidateAsync(user.Id, PasswordOtpPurpose.ForgotPassword, request.Otp, ct);
        if (otp is null) return InvalidCode();

        var token = await users.GeneratePasswordResetTokenAsync(user);
        var result = await users.ResetPasswordAsync(user, token, request.NewPassword);
        if (!result.Succeeded) return new(false, result.Errors.First().Description, result.Errors.Select(x => x.Description).ToArray());
        otp.UsedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new(true, "Password reset successfully. You can now sign in with your new password.");
    }

    public async Task<OperationResult> ChangeAsync(string userId, ChangePasswordOtpRequest request, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(userId);
        if (user is null) return new(false, "Account not found.");
        if (!await users.CheckPasswordAsync(user, request.CurrentPassword)) return new(false, "Current password is incorrect.");

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var otp = await ValidateAsync(user.Id, PasswordOtpPurpose.ChangePassword, request.Otp, ct);
        if (otp is null) return InvalidCode();
        var result = await users.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded) return new(false, result.Errors.First().Description, result.Errors.Select(x => x.Description).ToArray());
        otp.UsedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new(true, "Password changed successfully. Please sign in again on your other devices.");
    }

    private async Task<EmailDeliveryResult> CreateAndSendAsync(ApplicationUser user, PasswordOtpPurpose purpose, string action, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var latest = await db.PasswordOtps.Where(x => x.UserId == user.Id && x.Purpose == purpose)
            .OrderByDescending(x => x.CreatedAtUtc).FirstOrDefaultAsync(ct);
        if (latest is not null && latest.CreatedAtUtc > now - ResendCooldown)
            return new(false, "Please wait one minute before requesting another code.", user.Email);

        var active = await db.PasswordOtps.Where(x => x.UserId == user.Id && x.Purpose == purpose && x.UsedAtUtc == null).ToListAsync(ct);
        foreach (var item in active) item.UsedAtUtc = now;

        var code = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        var saltBytes = RandomNumberGenerator.GetBytes(16);
        var salt = Convert.ToHexString(saltBytes);
        db.PasswordOtps.Add(new PasswordOtp
        {
            UserId = user.Id, Purpose = purpose, Salt = salt,
            CodeHash = Hash(code, salt), CreatedAtUtc = now, ExpiresAtUtc = now.Add(Lifetime)
        });
        await db.SaveChangesAsync(ct);

        var delivery = await email.SendPasswordOtpAsync(user.Email!, user.FirstName, code, action, ct);
        if (!delivery.Success)
        {
            var created = await db.PasswordOtps.Where(x => x.UserId == user.Id && x.Purpose == purpose && x.UsedAtUtc == null)
                .OrderByDescending(x => x.CreatedAtUtc).FirstAsync(ct);
            created.UsedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
        return delivery;
    }

    private async Task<PasswordOtp?> ValidateAsync(string userId, PasswordOtpPurpose purpose, string code, CancellationToken ct)
    {
        var otp = await db.PasswordOtps.Where(x => x.UserId == userId && x.Purpose == purpose && x.UsedAtUtc == null)
            .OrderByDescending(x => x.CreatedAtUtc).FirstOrDefaultAsync(ct);
        if (otp is null || otp.ExpiresAtUtc <= DateTime.UtcNow || otp.FailedAttempts >= MaxAttempts) return null;
        if (!CryptographicOperations.FixedTimeEquals(Convert.FromHexString(otp.CodeHash), Convert.FromHexString(Hash(code, otp.Salt))))
        {
            otp.FailedAttempts++;
            if (otp.FailedAttempts >= MaxAttempts) otp.UsedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return null;
        }
        return otp;
    }

    private static string Hash(string code, string salt) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{salt}:{code}")));
    private static OperationResult InvalidCode() => new(false, "The security code is invalid, expired, or has been used.");
}


