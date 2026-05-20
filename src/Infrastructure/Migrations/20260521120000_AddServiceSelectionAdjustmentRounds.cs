using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using sp26se058_3dprintshop_be.Infrastructure.Data;

#nullable disable

namespace sp26se058_3dprintshop_be.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260521120000_AddServiceSelectionAdjustmentRounds")]
    public partial class AddServiceSelectionAdjustmentRounds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AdjustmentRoundLimit",
                table: "ServiceSelections",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UsedAdjustmentRoundCount",
                table: "ServiceSelections",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceSelections_DesignWorkId_IsLocked_Created",
                table: "ServiceSelections",
                columns: new[] { "DesignWorkId", "IsLocked", "Created" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ServiceSelections_DesignWorkId_IsLocked_Created",
                table: "ServiceSelections");

            migrationBuilder.DropColumn(
                name: "AdjustmentRoundLimit",
                table: "ServiceSelections");

            migrationBuilder.DropColumn(
                name: "UsedAdjustmentRoundCount",
                table: "ServiceSelections");
        }
    }
}
