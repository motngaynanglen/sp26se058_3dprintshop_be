using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sp26se058_3dprintshop_be.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixDataBaseServiceOption : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Comment lại vì MySQL không tìm thấy Foreign Key này để xóa
            // migrationBuilder.DropForeignKey(
            //    name: "FK_DesignWorks_DesignTemplates_TemplateId",
            //    table: "DesignWorks");

            // ĐÂY LÀ DÒNG GÂY LỖI CHÍNH CỦA BẠN - ĐÃ COMMENT LẠI
            // migrationBuilder.DropForeignKey(
            //    name: "FK_ServiceSelections_ServicePackages_ServicePackageId",
            //    table: "ServiceSelections");

            migrationBuilder.DropTable(
                name: "PackageOptions");

            migrationBuilder.DropTable(
                name: "ServiceSelectionOptions");

            migrationBuilder.DropTable(
                name: "ServicePackages");

            migrationBuilder.DropIndex(
                name: "IX_ServiceSelections_ServicePackageId",
                table: "ServiceSelections");

            migrationBuilder.DropIndex(
                name: "IX_DesignWorks_TemplateId",
                table: "DesignWorks");

            migrationBuilder.DropColumn(
                name: "SelectionType",
                table: "ServiceSelections");

            migrationBuilder.DropColumn(
                name: "ServicePackageId",
                table: "ServiceSelections");

            migrationBuilder.DropColumn(
                name: "OptionType",
                table: "ServiceOptions");

            migrationBuilder.DropColumn(
                name: "SourceType",
                table: "DesignWorks");

            migrationBuilder.DropColumn(
                name: "TemplateId",
                table: "DesignWorks");

            migrationBuilder.AddColumn<decimal>(
                name: "TotalPrice",
                table: "ServiceSelections",
                type: "decimal(18,2)", // Đã chỉnh lại cho chuẩn tiền tệ
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "ServiceSelectedOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ServiceSelectionId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ServiceOptionId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    OptionNameSnapshot = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Quantity = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    AppliedPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastModified = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Deleted = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    DeletedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceSelectedOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceSelectedOptions_ServiceOptions_ServiceOptionId",
                        column: x => x.ServiceOptionId,
                        principalTable: "ServiceOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceSelectedOptions_ServiceSelections_ServiceSelectionId",
                        column: x => x.ServiceSelectionId,
                        principalTable: "ServiceSelections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceSelectedOptions_Deleted",
                table: "ServiceSelectedOptions",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceSelectedOptions_ServiceOptionId",
                table: "ServiceSelectedOptions",
                column: "ServiceOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceSelectedOptions_ServiceSelectionId",
                table: "ServiceSelectedOptions",
                column: "ServiceSelectionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceSelectedOptions");

            migrationBuilder.DropColumn(
                name: "TotalPrice",
                table: "ServiceSelections");

            migrationBuilder.AddColumn<string>(
                name: "SelectionType",
                table: "ServiceSelections",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<Guid>(
                name: "ServicePackageId",
                table: "ServiceSelections",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "OptionType",
                table: "ServiceOptions",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "SourceType",
                table: "DesignWorks",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<Guid>(
                name: "TemplateId",
                table: "DesignWorks",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.CreateTable(
                name: "ServicePackages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    BasePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Code = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Created = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Deleted = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    DeletedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LastModified = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ServiceType = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServicePackages", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ServiceSelectionOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ServiceOptionId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ServiceSelectionId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    AppliedPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Deleted = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    DeletedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastModified = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Quantity = table.Column<int>(type: "int", nullable: false, defaultValue: 1)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceSelectionOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceSelectionOptions_ServiceOptions_ServiceOptionId",
                        column: x => x.ServiceOptionId,
                        principalTable: "ServiceOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceSelectionOptions_ServiceSelections_ServiceSelectionId",
                        column: x => x.ServiceSelectionId,
                        principalTable: "ServiceSelections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PackageOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ServiceOptionId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ServicePackageId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Created = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DefaultSelected = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Deleted = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: true),
                    DeletedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsRequired = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LastModified = table.Column<DateTimeOffset>(type: "datetime(6)", nullable: false),
                    LastModifiedBy = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MaxQuantity = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    MinQuantity = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    PriceOverride = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackageOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PackageOptions_ServiceOptions_ServiceOptionId",
                        column: x => x.ServiceOptionId,
                        principalTable: "ServiceOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PackageOptions_ServicePackages_ServicePackageId",
                        column: x => x.ServicePackageId,
                        principalTable: "ServicePackages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceSelections_ServicePackageId",
                table: "ServiceSelections",
                column: "ServicePackageId");

            migrationBuilder.CreateIndex(
                name: "IX_DesignWorks_TemplateId",
                table: "DesignWorks",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageOptions_Deleted",
                table: "PackageOptions",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_PackageOptions_ServiceOptionId",
                table: "PackageOptions",
                column: "ServiceOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_PackageOptions_ServicePackageId",
                table: "PackageOptions",
                column: "ServicePackageId");

            migrationBuilder.CreateIndex(
                name: "IX_ServicePackages_Code",
                table: "ServicePackages",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServicePackages_Deleted",
                table: "ServicePackages",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceSelectionOptions_Deleted",
                table: "ServiceSelectionOptions",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceSelectionOptions_ServiceOptionId",
                table: "ServiceSelectionOptions",
                column: "ServiceOptionId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceSelectionOptions_ServiceSelectionId",
                table: "ServiceSelectionOptions",
                column: "ServiceSelectionId");

            migrationBuilder.AddForeignKey(
                name: "FK_DesignWorks_DesignTemplates_TemplateId",
                table: "DesignWorks",
                column: "TemplateId",
                principalTable: "DesignTemplates",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceSelections_ServicePackages_ServicePackageId",
                table: "ServiceSelections",
                column: "ServicePackageId",
                principalTable: "ServicePackages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
