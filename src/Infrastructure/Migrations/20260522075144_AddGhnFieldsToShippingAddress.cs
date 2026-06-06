using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sp26se058_3dprintshop_be.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddGhnFieldsToShippingAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GhnDistrictId",
                table: "ShippingAddress",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GhnWardCode",
                table: "ShippingAddress",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GhnDistrictId",
                table: "ShippingAddress");

            migrationBuilder.DropColumn(
                name: "GhnWardCode",
                table: "ShippingAddress");
        }
    }
}
