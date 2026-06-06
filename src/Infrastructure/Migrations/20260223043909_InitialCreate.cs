using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sp26se058_3dprintshop_be.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Username = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Fullname = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ProfileImageURL = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ContactPhone = table.Column<string>(type: "varchar(15)", maxLength: 15, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ZaloPhone = table.Column<string>(type: "varchar(15)", maxLength: 15, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PasswordHash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
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
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ConceptTags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsMainTag = table.Column<bool>(type: "tinyint(1)", nullable: false),
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
                    table.PrimaryKey("PK_ConceptTags", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DesignTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FileUrl = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ThumbnailUrl = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
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
                    table.PrimaryKey("PK_DesignTemplates", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Materials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
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
                    table.PrimaryKey("PK_Materials", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ServicePackage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Code = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    BasePrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsSupported = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    HtmlRaw = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
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
                    table.PrimaryKey("PK_ServicePackage", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    AccountId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DateOfBirth = table.Column<DateTime>(type: "datetime(6)", nullable: true),
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
                    table.PrimaryKey("PK_Customers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Customers_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Managers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    AccountId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
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
                    table.PrimaryKey("PK_Managers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Managers_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Staffs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    AccountId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
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
                    table.PrimaryKey("PK_Staffs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Staffs_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DesignTags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    ConceptTagId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DesignTemplateId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    IsMainTag = table.Column<bool>(type: "tinyint(1)", nullable: false),
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
                    table.PrimaryKey("PK_DesignTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DesignTags_ConceptTags_ConceptTagId",
                        column: x => x.ConceptTagId,
                        principalTable: "ConceptTags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DesignTags_DesignTemplates_DesignTemplateId",
                        column: x => x.DesignTemplateId,
                        principalTable: "DesignTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DesignVariants",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PreviewModelUrl = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DesignTemplateId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    MaterialId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    SizeScale = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    StockQuantity = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    MinimumStockLevel = table.Column<int>(type: "int", nullable: true),
                    IsAllowPreOrder = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    EstimatedWeightPerUnit = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: true),
                    EstimatedPrintTimePerUnit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MarkupPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false, defaultValue: 0m),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
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
                    table.PrimaryKey("PK_DesignVariants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DesignVariants_DesignTemplates_DesignTemplateId",
                        column: x => x.DesignTemplateId,
                        principalTable: "DesignTemplates",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DesignVariants_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MaterialPriceHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    MaterialId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    BaseCostPerGram = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalServiceCostPerGram = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    EffectiveDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsCurrent = table.Column<bool>(type: "tinyint(1)", nullable: false),
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
                    table.PrimaryKey("PK_MaterialPriceHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaterialPriceHistories_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DesignWorks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ServicePackageId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    SourceType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TemplateId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    CustomerId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    MainAssignedStaffId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    BaseImageUrl = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ResultDraftId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    Status = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
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
                    table.PrimaryKey("PK_DesignWorks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DesignWorks_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DesignWorks_DesignTemplates_TemplateId",
                        column: x => x.TemplateId,
                        principalTable: "DesignTemplates",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DesignWorks_ServicePackage_ServicePackageId",
                        column: x => x.ServicePackageId,
                        principalTable: "ServicePackage",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DesignWorks_Staffs_MainAssignedStaffId",
                        column: x => x.MainAssignedStaffId,
                        principalTable: "Staffs",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Code = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CustomerId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    StaffId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    OrderStatus = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "PENDING")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Priority = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orders_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_Staffs_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staffs",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DesignLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DesignWorkId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    IsAI = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    AccountId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    Content = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Metadata = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LogType = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
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
                    table.PrimaryKey("PK_DesignLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DesignLogs_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DesignLogs_DesignWorks_DesignWorkId",
                        column: x => x.DesignWorkId,
                        principalTable: "DesignWorks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Invoices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    OrderId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    InvoiceCode = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SubTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TaxAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ShippingFee = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentStatus = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "UNPAID")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DueDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
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
                    table.PrimaryKey("PK_Invoices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Invoices_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DesignVersionHistorys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Tilte = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DesignWorkId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DesignLogId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    UploaderId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    FileUrl = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    IsPreviewable = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsPrintable = table.Column<bool>(type: "tinyint(1)", nullable: false),
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
                    table.PrimaryKey("PK_DesignVersionHistorys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DesignVersionHistorys_Accounts_UploaderId",
                        column: x => x.UploaderId,
                        principalTable: "Accounts",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DesignVersionHistorys_DesignLogs_DesignLogId",
                        column: x => x.DesignLogId,
                        principalTable: "DesignLogs",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DesignVersionHistorys_DesignWorks_DesignWorkId",
                        column: x => x.DesignWorkId,
                        principalTable: "DesignWorks",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    InvoiceId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentMethod = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ExternalTransactionId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Note = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TransactionStatus = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false, defaultValue: "PENDING")
                        .Annotation("MySql:CharSet", "utf8mb4"),
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
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transactions_Invoices_InvoiceId",
                        column: x => x.InvoiceId,
                        principalTable: "Invoices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TechnicalDrafts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PreviewModelUrl = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    InfillDensity = table.Column<int>(type: "int", nullable: false),
                    LayerHeight = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    EstimatedWeightPerUnit = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    EstimatedPrintTimePerUnit = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    MarkupPercentage = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TechnicalNote = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DesignVersionHistoryId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    MaterialId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
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
                    table.PrimaryKey("PK_TechnicalDrafts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TechnicalDrafts_DesignVersionHistorys_DesignVersionHistoryId",
                        column: x => x.DesignVersionHistoryId,
                        principalTable: "DesignVersionHistorys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TechnicalDrafts_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "OrderItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    OrderId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    SourceType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DesignWorkId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    DesignVariantId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    TechnicalDraftId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    QuantityOrdered = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FulfillmentStatus = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
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
                    table.PrimaryKey("PK_OrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrderItems_DesignVariants_DesignVariantId",
                        column: x => x.DesignVariantId,
                        principalTable: "DesignVariants",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OrderItems_DesignWorks_DesignWorkId",
                        column: x => x.DesignWorkId,
                        principalTable: "DesignWorks",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OrderItems_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrderItems_TechnicalDrafts_TechnicalDraftId",
                        column: x => x.TechnicalDraftId,
                        principalTable: "TechnicalDrafts",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "InventoryTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    DesignVariantId = table.Column<Guid>(type: "char(36)", nullable: false, collation: "ascii_general_ci"),
                    StaffId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    Type = table.Column<string>(type: "varchar(30)", maxLength: 30, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    ReferenceId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
                    Note = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OrderItemId = table.Column<Guid>(type: "char(36)", nullable: true, collation: "ascii_general_ci"),
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
                    table.PrimaryKey("PK_InventoryTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_DesignVariants_DesignVariantId",
                        column: x => x.DesignVariantId,
                        principalTable: "DesignVariants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_OrderItems_OrderItemId",
                        column: x => x.OrderItemId,
                        principalTable: "OrderItems",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_Staffs_StaffId",
                        column: x => x.StaffId,
                        principalTable: "Staffs",
                        principalColumn: "Id");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Deleted",
                table: "Accounts",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Email",
                table: "Accounts",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Username",
                table: "Accounts",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConceptTags_Deleted",
                table: "ConceptTags",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_ConceptTags_Name",
                table: "ConceptTags",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_AccountId",
                table: "Customers",
                column: "AccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Deleted",
                table: "Customers",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_DesignLogs_AccountId",
                table: "DesignLogs",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_DesignLogs_Deleted",
                table: "DesignLogs",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_DesignLogs_DesignWorkId",
                table: "DesignLogs",
                column: "DesignWorkId");

            migrationBuilder.CreateIndex(
                name: "IX_DesignTags_ConceptTagId",
                table: "DesignTags",
                column: "ConceptTagId");

            migrationBuilder.CreateIndex(
                name: "IX_DesignTags_Deleted",
                table: "DesignTags",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_DesignTags_DesignTemplateId",
                table: "DesignTags",
                column: "DesignTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_DesignTemplates_Code",
                table: "DesignTemplates",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DesignTemplates_Deleted",
                table: "DesignTemplates",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_DesignVariants_Code",
                table: "DesignVariants",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DesignVariants_Deleted",
                table: "DesignVariants",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_DesignVariants_DesignTemplateId",
                table: "DesignVariants",
                column: "DesignTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_DesignVariants_MaterialId",
                table: "DesignVariants",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_DesignVersionHistorys_Deleted",
                table: "DesignVersionHistorys",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_DesignVersionHistorys_DesignLogId",
                table: "DesignVersionHistorys",
                column: "DesignLogId");

            migrationBuilder.CreateIndex(
                name: "IX_DesignVersionHistorys_DesignWorkId",
                table: "DesignVersionHistorys",
                column: "DesignWorkId");

            migrationBuilder.CreateIndex(
                name: "IX_DesignVersionHistorys_UploaderId",
                table: "DesignVersionHistorys",
                column: "UploaderId");

            migrationBuilder.CreateIndex(
                name: "IX_DesignWorks_CustomerId",
                table: "DesignWorks",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_DesignWorks_Deleted",
                table: "DesignWorks",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_DesignWorks_MainAssignedStaffId",
                table: "DesignWorks",
                column: "MainAssignedStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_DesignWorks_ServicePackageId",
                table: "DesignWorks",
                column: "ServicePackageId");

            migrationBuilder.CreateIndex(
                name: "IX_DesignWorks_TemplateId",
                table: "DesignWorks",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_Deleted",
                table: "InventoryTransactions",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_DesignVariantId",
                table: "InventoryTransactions",
                column: "DesignVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_OrderItemId",
                table: "InventoryTransactions",
                column: "OrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_StaffId",
                table: "InventoryTransactions",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_Deleted",
                table: "Invoices",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_InvoiceCode",
                table: "Invoices",
                column: "InvoiceCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_OrderId",
                table: "Invoices",
                column: "OrderId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Managers_AccountId",
                table: "Managers",
                column: "AccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Managers_Deleted",
                table: "Managers",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialPriceHistories_Deleted",
                table: "MaterialPriceHistories",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialPriceHistories_MaterialId",
                table: "MaterialPriceHistories",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_Materials_Deleted",
                table: "Materials",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_Deleted",
                table: "OrderItems",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_DesignVariantId",
                table: "OrderItems",
                column: "DesignVariantId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_DesignWorkId",
                table: "OrderItems",
                column: "DesignWorkId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_OrderId",
                table: "OrderItems",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_TechnicalDraftId",
                table: "OrderItems",
                column: "TechnicalDraftId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerId",
                table: "Orders",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Deleted",
                table: "Orders",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_StaffId",
                table: "Orders",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_ServicePackage_Code",
                table: "ServicePackage",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServicePackage_Deleted",
                table: "ServicePackage",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_Staffs_AccountId",
                table: "Staffs",
                column: "AccountId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Staffs_Deleted",
                table: "Staffs",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalDrafts_Deleted",
                table: "TechnicalDrafts",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalDrafts_DesignVersionHistoryId",
                table: "TechnicalDrafts",
                column: "DesignVersionHistoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalDrafts_MaterialId",
                table: "TechnicalDrafts",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_Deleted",
                table: "Transactions",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ExternalTransactionId",
                table: "Transactions",
                column: "ExternalTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_InvoiceId",
                table: "Transactions",
                column: "InvoiceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DesignTags");

            migrationBuilder.DropTable(
                name: "InventoryTransactions");

            migrationBuilder.DropTable(
                name: "Managers");

            migrationBuilder.DropTable(
                name: "MaterialPriceHistories");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "ConceptTags");

            migrationBuilder.DropTable(
                name: "OrderItems");

            migrationBuilder.DropTable(
                name: "Invoices");

            migrationBuilder.DropTable(
                name: "DesignVariants");

            migrationBuilder.DropTable(
                name: "TechnicalDrafts");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "DesignVersionHistorys");

            migrationBuilder.DropTable(
                name: "Materials");

            migrationBuilder.DropTable(
                name: "DesignLogs");

            migrationBuilder.DropTable(
                name: "DesignWorks");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropTable(
                name: "DesignTemplates");

            migrationBuilder.DropTable(
                name: "ServicePackage");

            migrationBuilder.DropTable(
                name: "Staffs");

            migrationBuilder.DropTable(
                name: "Accounts");
        }
    }
}
