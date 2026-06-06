using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sp26se058_3dprintshop_be.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSourceDesignWorkId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SourceDesignWorkId",
                table: "DesignWorks",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateIndex(
                name: "IX_DesignWorks_SourceDesignWorkId",
                table: "DesignWorks",
                column: "SourceDesignWorkId");

            migrationBuilder.AddForeignKey(
                name: "FK_DesignWorks_DesignWorks_SourceDesignWorkId",
                table: "DesignWorks",
                column: "SourceDesignWorkId",
                principalTable: "DesignWorks",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DesignWorks_DesignWorks_SourceDesignWorkId",
                table: "DesignWorks");

            migrationBuilder.DropIndex(
                name: "IX_DesignWorks_SourceDesignWorkId",
                table: "DesignWorks");

            migrationBuilder.DropColumn(
                name: "SourceDesignWorkId",
                table: "DesignWorks");
        }
    }
}
