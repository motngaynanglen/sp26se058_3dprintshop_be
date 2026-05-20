using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using sp26se058_3dprintshop_be.Infrastructure.Data;

#nullable disable

namespace sp26se058_3dprintshop_be.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260521123000_AddAdjustmentRoundDeltaToServiceOptions")]
    public partial class AddAdjustmentRoundDeltaToServiceOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AdjustmentRoundDelta",
                table: "ServiceOptions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AdjustmentRoundDeltaSnapshot",
                table: "ServiceSelectedOptions",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AdjustmentRoundDelta",
                table: "ServiceOptions");

            migrationBuilder.DropColumn(
                name: "AdjustmentRoundDeltaSnapshot",
                table: "ServiceSelectedOptions");
        }
    }
}
