using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using sp26se058_3dprintshop_be.Infrastructure.Data;

#nullable disable

namespace sp26se058_3dprintshop_be.Infrastructure.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260521124500_AddAdjustmentReviewFieldsToDesignLogs")]
    public partial class AddAdjustmentReviewFieldsToDesignLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AdjustmentConsumedServiceSelectionId",
                table: "DesignLogs",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "AdjustmentDecisionNote",
                table: "DesignLogs",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AdjustmentReviewedAt",
                table: "DesignLogs",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "AdjustmentReviewedByAccountId",
                table: "DesignLogs",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "AdjustmentRequestStatus",
                table: "DesignLogs",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_DesignLogs_DesignWorkId_LogType_AdjustmentRequestStatus",
                table: "DesignLogs",
                columns: new[] { "DesignWorkId", "LogType", "AdjustmentRequestStatus" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DesignLogs_DesignWorkId_LogType_AdjustmentRequestStatus",
                table: "DesignLogs");

            migrationBuilder.DropColumn(
                name: "AdjustmentConsumedServiceSelectionId",
                table: "DesignLogs");

            migrationBuilder.DropColumn(
                name: "AdjustmentDecisionNote",
                table: "DesignLogs");

            migrationBuilder.DropColumn(
                name: "AdjustmentReviewedAt",
                table: "DesignLogs");

            migrationBuilder.DropColumn(
                name: "AdjustmentReviewedByAccountId",
                table: "DesignLogs");

            migrationBuilder.DropColumn(
                name: "AdjustmentRequestStatus",
                table: "DesignLogs");
        }
    }
}
