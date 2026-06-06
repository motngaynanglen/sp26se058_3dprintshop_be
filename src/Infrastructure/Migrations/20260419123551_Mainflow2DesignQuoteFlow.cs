using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sp26se058_3dprintshop_be.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Mainflow2DesignQuoteFlow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DesignWorks_ServicePackages_ServicePackageId",
                table: "DesignWorks");

            migrationBuilder.AlterColumn<Guid>(
                name: "ServicePackageId",
                table: "DesignWorks",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci",
                oldClrType: typeof(Guid),
                oldType: "char(36)")
                .OldAnnotation("Relational:Collation", "ascii_general_ci");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CustomerApprovedAt",
                table: "DesignWorks",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InitialIdeaImageUrlsJson",
                table: "DesignWorks",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastQuotedAt",
                table: "DesignWorks",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LatestQuotedPrice",
                table: "DesignWorks",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "QuoteRevision",
                table: "DesignWorks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RequirementBrief",
                table: "DesignWorks",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StaffAssignedAt",
                table: "DesignWorks",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DesignWorks_ServicePackages_ServicePackageId",
                table: "DesignWorks",
                column: "ServicePackageId",
                principalTable: "ServicePackages",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DesignWorks_ServicePackages_ServicePackageId",
                table: "DesignWorks");

            migrationBuilder.DropColumn(
                name: "CustomerApprovedAt",
                table: "DesignWorks");

            migrationBuilder.DropColumn(
                name: "InitialIdeaImageUrlsJson",
                table: "DesignWorks");

            migrationBuilder.DropColumn(
                name: "LastQuotedAt",
                table: "DesignWorks");

            migrationBuilder.DropColumn(
                name: "LatestQuotedPrice",
                table: "DesignWorks");

            migrationBuilder.DropColumn(
                name: "QuoteRevision",
                table: "DesignWorks");

            migrationBuilder.DropColumn(
                name: "RequirementBrief",
                table: "DesignWorks");

            migrationBuilder.DropColumn(
                name: "StaffAssignedAt",
                table: "DesignWorks");

            migrationBuilder.AlterColumn<Guid>(
                name: "ServicePackageId",
                table: "DesignWorks",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci",
                oldClrType: typeof(Guid),
                oldType: "char(36)",
                oldNullable: true)
                .OldAnnotation("Relational:Collation", "ascii_general_ci");

            migrationBuilder.AddForeignKey(
                name: "FK_DesignWorks_ServicePackages_ServicePackageId",
                table: "DesignWorks",
                column: "ServicePackageId",
                principalTable: "ServicePackages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
