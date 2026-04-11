using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sp26se058_3dprintshop_be.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixFieldShipmentTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShippingMethodId",
                table: "Shipments");

            migrationBuilder.AddColumn<string>(
                name: "CarrierName",
                table: "Shipments",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CarrierName",
                table: "Shipments");

            migrationBuilder.AddColumn<Guid>(
                name: "ShippingMethodId",
                table: "Shipments",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");
        }
    }
}
