using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BusSeatBookingSystem.Repository.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingPassengerDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PassengerEmail",
                table: "Bookings",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PassengerName",
                table: "Bookings",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PassengerPhone",
                table: "Bookings",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql("""
                UPDATE b
                SET b.PassengerName = LTRIM(RTRIM(CONCAT(u.FirstName, ' ', u.LastName))),
                    b.PassengerEmail = COALESCE(u.Email, ''),
                    b.PassengerPhone = COALESCE(NULLIF(u.PhoneNumber, ''), 'Not provided')
                FROM Bookings b
                INNER JOIN AspNetUsers u ON u.Id = b.UserId;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PassengerEmail",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "PassengerName",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "PassengerPhone",
                table: "Bookings");
        }
    }
}

