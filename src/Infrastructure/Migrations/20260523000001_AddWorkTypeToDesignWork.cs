using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using sp26se058_3dprintshop_be.Infrastructure.Data;

#nullable disable

namespace sp26se058_3dprintshop_be.Infrastructure.Migrations
{
    /// <summary>
    /// ADR-004: Add WorkType to DesignWork to distinguish Design Service ("DESIGN_SERVICE")
    /// from POD/Quick Print ("PRINT_SERVICE"). Nullable — existing rows default to null
    /// which is treated as "DESIGN_SERVICE" by all query filters and commands.
    /// </summary>
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260523000001_AddWorkTypeToDesignWork")]
    public partial class AddWorkTypeToDesignWork : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WorkType",
                table: "DesignWorks",
                type: "varchar(30)",
                maxLength: 30,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            // Index to support staff dashboard filters: "show only PRINT_SERVICE works"
            migrationBuilder.CreateIndex(
                name: "IX_DesignWorks_WorkType",
                table: "DesignWorks",
                column: "WorkType");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DesignWorks_WorkType",
                table: "DesignWorks");

            migrationBuilder.DropColumn(
                name: "WorkType",
                table: "DesignWorks");
        }
    }
}
