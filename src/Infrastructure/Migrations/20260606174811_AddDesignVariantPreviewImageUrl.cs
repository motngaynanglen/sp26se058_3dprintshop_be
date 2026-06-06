using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sp26se058_3dprintshop_be.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDesignVariantPreviewImageUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PreviewImageUrl",
                table: "DesignVariants",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreviewImageUrl",
                table: "DesignVariants");
        }
    }
}
