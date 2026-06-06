using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sp26se058_3dprintshop_be.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueContactPhoneIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Accounts_ContactPhone",
                table: "Accounts",
                column: "ContactPhone",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Accounts_ContactPhone",
                table: "Accounts");
        }
    }
}
