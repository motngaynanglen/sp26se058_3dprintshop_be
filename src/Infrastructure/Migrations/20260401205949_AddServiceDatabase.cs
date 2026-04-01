using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sp26se058_3dprintshop_be.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceDatabase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DesignWorks_ServicePackage_ServicePackageId",
                table: "DesignWorks");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ServicePackage",
                table: "ServicePackage");

            migrationBuilder.DropColumn(
                name: "HtmlRaw",
                table: "ServicePackage");

            migrationBuilder.RenameTable(
                name: "ServicePackage",
                newName: "ServicePackages");

            migrationBuilder.RenameColumn(
                name: "IsSupported",
                table: "ServicePackages",
                newName: "IsActive");

            migrationBuilder.RenameIndex(
                name: "IX_ServicePackage_Deleted",
                table: "ServicePackages",
                newName: "IX_ServicePackages_Deleted");

            migrationBuilder.RenameIndex(
                name: "IX_ServicePackage_Code",
                table: "ServicePackages",
                newName: "IX_ServicePackages_Code");

            migrationBuilder.AddColumn<Guid>(
                name: "ServiceSelectionId",
                table: "OrderItems",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

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

            migrationBuilder.AddColumn<Guid>(
                name: "ServiceSelectionId",
                table: "DesignWorks",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "ServiceType",
                table: "ServicePackages",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ServicePackages",
                table: "ServicePackages",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "ServiceOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OptionType = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DefaultPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
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
                    table.PrimaryKey("PK_ServiceOptions", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ServiceSelections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DesignWorkId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ServicePackageId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    SelectionType = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsLocked = table.Column<bool>(type: "tinyint(1)", nullable: false),
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
                    table.PrimaryKey("PK_ServiceSelections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceSelections_DesignWorks_DesignWorkId",
                        column: x => x.DesignWorkId,
                        principalTable: "DesignWorks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiceSelections_ServicePackages_ServicePackageId",
                        column: x => x.ServicePackageId,
                        principalTable: "ServicePackages",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PackageOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ServicePackageId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ServiceOptionId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    IsRequired = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    DefaultSelected = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    PriceOverride = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MinQuantity = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    MaxQuantity = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
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

            migrationBuilder.CreateTable(
                name: "ServiceSelectionOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ServiceSelectionId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ServiceOptionId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
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

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_ServiceSelectionId",
                table: "OrderItems",
                column: "ServiceSelectionId");

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
                name: "IX_ServiceOptions_Code",
                table: "ServiceOptions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOptions_Deleted",
                table: "ServiceOptions",
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

            migrationBuilder.CreateIndex(
                name: "IX_ServiceSelections_Deleted",
                table: "ServiceSelections",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceSelections_DesignWorkId",
                table: "ServiceSelections",
                column: "DesignWorkId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceSelections_ServicePackageId",
                table: "ServiceSelections",
                column: "ServicePackageId");

            migrationBuilder.AddForeignKey(
                name: "FK_DesignWorks_ServicePackages_ServicePackageId",
                table: "DesignWorks",
                column: "ServicePackageId",
                principalTable: "ServicePackages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_ServiceSelections_ServiceSelectionId",
                table: "OrderItems",
                column: "ServiceSelectionId",
                principalTable: "ServiceSelections",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DesignWorks_ServicePackages_ServicePackageId",
                table: "DesignWorks");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_ServiceSelections_ServiceSelectionId",
                table: "OrderItems");

            migrationBuilder.DropTable(
                name: "PackageOptions");

            migrationBuilder.DropTable(
                name: "ServiceSelectionOptions");

            migrationBuilder.DropTable(
                name: "ServiceOptions");

            migrationBuilder.DropTable(
                name: "ServiceSelections");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_ServiceSelectionId",
                table: "OrderItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ServicePackages",
                table: "ServicePackages");

            migrationBuilder.DropColumn(
                name: "ServiceSelectionId",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "ServiceSelectionId",
                table: "DesignWorks");

            migrationBuilder.DropColumn(
                name: "ServiceType",
                table: "ServicePackages");

            migrationBuilder.RenameTable(
                name: "ServicePackages",
                newName: "ServicePackage");

            migrationBuilder.RenameColumn(
                name: "IsActive",
                table: "ServicePackage",
                newName: "IsSupported");

            migrationBuilder.RenameIndex(
                name: "IX_ServicePackages_Deleted",
                table: "ServicePackage",
                newName: "IX_ServicePackage_Deleted");

            migrationBuilder.RenameIndex(
                name: "IX_ServicePackages_Code",
                table: "ServicePackage",
                newName: "IX_ServicePackage_Code");

            migrationBuilder.AlterColumn<Guid>(
                name: "ServicePackageId",
                table: "DesignWorks",
                type: "char(36)",
                nullable: true,
                collation: "ascii_general_ci",
                oldClrType: typeof(Guid),
                oldType: "char(36)")
                .OldAnnotation("Relational:Collation", "ascii_general_ci");

            migrationBuilder.AddColumn<string>(
                name: "HtmlRaw",
                table: "ServicePackage",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ServicePackage",
                table: "ServicePackage",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DesignWorks_ServicePackage_ServicePackageId",
                table: "DesignWorks",
                column: "ServicePackageId",
                principalTable: "ServicePackage",
                principalColumn: "Id");
        }
    }
}
