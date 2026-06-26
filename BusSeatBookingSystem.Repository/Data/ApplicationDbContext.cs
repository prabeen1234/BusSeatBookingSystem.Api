using BusSeatBookingSystem.Entity.Entities.BusEntities;
using BusSeatBookingSystem.Entity.Entities.UserEntities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BusSeatBookingSystem.Repository.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<BusDetails> BusDetails => Set<BusDetails>();
    public DbSet<BusRoute> BusRoutes => Set<BusRoute>();
    public DbSet<BusSeat> BusSeats => Set<BusSeat>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingSeat> BookingSeats => Set<BookingSeat>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PasswordOtp> PasswordOtps => Set<PasswordOtp>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<BusDetails>().HasIndex(x => x.BusNumber).IsUnique();
        modelBuilder.Entity<BusDetails>().Property(x => x.Fare).HasPrecision(18, 2);
        modelBuilder.Entity<BusSeat>().HasIndex(x => new { x.BusId, x.SeatNumber }).IsUnique();
        modelBuilder.Entity<Booking>().HasIndex(x => x.BookingNumber).IsUnique();
        modelBuilder.Entity<Booking>().Property(x => x.TotalAmount).HasPrecision(18, 2);
        modelBuilder.Entity<BookingSeat>().HasKey(x => new { x.BookingId, x.SeatId });
        modelBuilder.Entity<BookingSeat>().Property(x => x.Fare).HasPrecision(18, 2);
        modelBuilder.Entity<BookingSeat>().HasOne(x => x.Seat).WithMany(x => x.BookingSeats).HasForeignKey(x => x.SeatId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Payment>().HasIndex(x => x.TransactionUuid).IsUnique();
        modelBuilder.Entity<Payment>().Property(x => x.Amount).HasPrecision(18, 2);
        modelBuilder.Entity<Booking>().HasOne(x => x.User).WithMany(x => x.Bookings).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<PasswordOtp>().HasIndex(x => new { x.UserId, x.Purpose, x.CreatedAtUtc });
        modelBuilder.Entity<PasswordOtp>().HasOne(x => x.User).WithMany(x => x.PasswordOtps).HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
