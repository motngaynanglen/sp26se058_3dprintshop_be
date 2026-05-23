using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using sp26se058_3dprintshop_be.Infrastructure.Data;

#nullable disable

namespace sp26se058_3dprintshop_be.Infrastructure.Migrations
{
    /// <summary>
    /// ADR-004 Option C: Add FileReviewStatus and FileReviewNote to DesignVersionHistory.
    /// These fields replace the boolean IsPrintable with a richer PENDING/ACCEPTED/REJECTED
    /// status used by both Design Service and POD (Quick Print) flows.
    /// IsPrintable is kept for backward compatibility — always synced by ReviewFileVersionCommand.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260523000000_AddFileReviewStatusToDesignVersionHistory")]
    public partial class AddFileReviewStatusToDesignVersionHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FileReviewStatus",
                table: "DesignVersionHistorys",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "PENDING")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "FileReviewNote",
                table: "DesignVersionHistorys",
                type: "varchar(2000)",
                maxLength: 2000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            // Index for staff dashboard: quickly find all files pending review for a DesignWork
            migrationBuilder.CreateIndex(
                name: "IX_DesignVersionHistorys_DesignWorkId_FileReviewStatus",
                table: "DesignVersionHistorys",
                columns: new[] { "DesignWorkId", "FileReviewStatus" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DesignVersionHistorys_DesignWorkId_FileReviewStatus",
                table: "DesignVersionHistorys");

            migrationBuilder.DropColumn(
                name: "FileReviewStatus",
                table: "DesignVersionHistorys");

            migrationBuilder.DropColumn(
                name: "FileReviewNote",
                table: "DesignVersionHistorys");
        }
    }
}
