using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sp26se058_3dprintshop_be.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateShipment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ShipmentStatus",
                table: "Shipments",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "PREPARING",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50,
                oldDefaultValue: "PENDING")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "AddressLine",
                table: "Shipments",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "N/A")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Shipments",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "N/A")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "District",
                table: "Shipments",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "N/A")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "Shipments",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Province",
                table: "Shipments",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "N/A")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "RecipientName",
                table: "Shipments",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "N/A")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "RecipientPhone",
                table: "Shipments",
                type: "varchar(15)",
                maxLength: 15,
                nullable: false,
                defaultValue: "N/A")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Ward",
                table: "Shipments",
                type: "varchar(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "N/A")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ShipmentAddressChangeRequests",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ShipmentId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    RequestedByCustomerId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    NewShippingAddressId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ReviewedByAccountId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    Status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false, defaultValue: "PENDING")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Reason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResponseNote = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    Created = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastModified = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Deleted = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    DeletedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentAddressChangeRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShipmentAddressChangeRequests_Accounts_ReviewedByAccountId",
                        column: x => x.ReviewedByAccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShipmentAddressChangeRequests_Customers_RequestedByCustomerId",
                        column: x => x.RequestedByCustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShipmentAddressChangeRequests_Shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ShipmentAddressChangeRequests_ShippingAddresses_NewShippingA~",
                        column: x => x.NewShippingAddressId,
                        principalTable: "ShippingAddresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentAddressChangeRequests_Deleted",
                table: "ShipmentAddressChangeRequests",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentAddressChangeRequests_NewShippingAddressId",
                table: "ShipmentAddressChangeRequests",
                column: "NewShippingAddressId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentAddressChangeRequests_RequestedByCustomerId",
                table: "ShipmentAddressChangeRequests",
                column: "RequestedByCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentAddressChangeRequests_ReviewedByAccountId",
                table: "ShipmentAddressChangeRequests",
                column: "ReviewedByAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentAddressChangeRequests_ShipmentId",
                table: "ShipmentAddressChangeRequests",
                column: "ShipmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShipmentAddressChangeRequests");

            migrationBuilder.DropColumn(
                name: "AddressLine",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "District",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "Province",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "RecipientName",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "RecipientPhone",
                table: "Shipments");

            migrationBuilder.DropColumn(
                name: "Ward",
                table: "Shipments");

            migrationBuilder.AlterColumn<string>(
                name: "ShipmentStatus",
                table: "Shipments",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "PENDING",
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50,
                oldDefaultValue: "PREPARING")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
