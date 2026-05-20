using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sp26se058_3dprintshop_be.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateServiceOption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "OptionNameSnapshot",
                table: "ServiceSelectedOptions",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "N/A",
                oldClrType: typeof(string),
                oldType: "longtext")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "OptionCodeSnapshot",
                table: "ServiceSelectedOptions",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "N/A")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "OptionDescriptionSnapshot",
                table: "ServiceSelectedOptions",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "OptionGroupCodeSnapshot",
                table: "ServiceSelectedOptions",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "OptionGroupNameSnapshot",
                table: "ServiceSelectedOptions",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "ServiceOptions",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "GroupCode",
                table: "ServiceOptions",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "GENERAL")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "GroupName",
                table: "ServiceOptions",
                type: "varchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "Chung")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "MaxQuantity",
                table: "ServiceOptions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MinQuantity",
                table: "ServiceOptions",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "SelectionType",
                table: "ServiceOptions",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "ADDON")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "ServiceOptions",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OptionCodeSnapshot",
                table: "ServiceSelectedOptions");

            migrationBuilder.DropColumn(
                name: "OptionDescriptionSnapshot",
                table: "ServiceSelectedOptions");

            migrationBuilder.DropColumn(
                name: "OptionGroupCodeSnapshot",
                table: "ServiceSelectedOptions");

            migrationBuilder.DropColumn(
                name: "OptionGroupNameSnapshot",
                table: "ServiceSelectedOptions");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "ServiceOptions");

            migrationBuilder.DropColumn(
                name: "GroupCode",
                table: "ServiceOptions");

            migrationBuilder.DropColumn(
                name: "GroupName",
                table: "ServiceOptions");

            migrationBuilder.DropColumn(
                name: "MaxQuantity",
                table: "ServiceOptions");

            migrationBuilder.DropColumn(
                name: "MinQuantity",
                table: "ServiceOptions");

            migrationBuilder.DropColumn(
                name: "SelectionType",
                table: "ServiceOptions");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "ServiceOptions");

            migrationBuilder.AlterColumn<string>(
                name: "OptionNameSnapshot",
                table: "ServiceSelectedOptions",
                type: "longtext",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "N/A")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
