using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static BusSeatBookingSystem.Entity.Entities.enums;

namespace BusSeatBookingSystem.Entity.Entities.UserEntities;

public class PasswordOtp
{
    [Key] public Guid PasswordOtpId { get; set; } = Guid.NewGuid();
    [Required] public string UserId { get; set; } = string.Empty;
    [ForeignKey(nameof(UserId))] public ApplicationUser User { get; set; } = null!;
    public PasswordOtpPurpose Purpose { get; set; }
    [Required, MaxLength(128)] public string CodeHash { get; set; } = string.Empty;
    [Required, MaxLength(64)] public string Salt { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? UsedAtUtc { get; set; }
    public int FailedAttempts { get; set; }
}
