using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Hotel_MVC.Migrations
{
    /// <inheritdoc />
    public partial class AddProfilePreferencesAndPaymobCard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AirportPickupPreference",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "BreakfastPreference",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "LateCheckoutPreference",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PaymobCardHolderName",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaymobCardSavedAt",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymobCardSubtype",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymobCardToken",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymobMaskedPan",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreferredRoomView",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SpecialRequests",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AirportPickupPreference",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "BreakfastPreference",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "LateCheckoutPreference",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PaymobCardHolderName",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PaymobCardSavedAt",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PaymobCardSubtype",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PaymobCardToken",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PaymobMaskedPan",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "PreferredRoomView",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "SpecialRequests",
                table: "AspNetUsers");
        }
    }
}
