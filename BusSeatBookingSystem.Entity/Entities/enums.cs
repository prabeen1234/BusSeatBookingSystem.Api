namespace BusSeatBookingSystem.Entity.Entities;

public class enums
{
    public enum SeatStatus { Available = 1, Booked = 2, Reserved = 3 }
    public enum Gender { Male = 1, Female = 2, Other = 3 }
    public enum BookingStatus { PendingPayment = 1, Confirmed = 2, Cancelled = 3, Expired = 4 }
    public enum PaymentStatus { Initiated = 1, Complete = 2, Failed = 3, Pending = 4, Refunded = 5 }
    public enum PasswordOtpPurpose { ForgotPassword = 1, ChangePassword = 2 }
}
