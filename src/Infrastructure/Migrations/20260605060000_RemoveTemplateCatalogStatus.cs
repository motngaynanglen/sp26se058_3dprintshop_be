using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sp26se058_3dprintshop_be.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTemplateCatalogStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Xóa index CatalogStatus trên DesignTemplates (nếu tồn tại)
            migrationBuilder.Sql(@"
                SET @exist := (SELECT COUNT(*) FROM INFORMATION_SCHEMA.STATISTICS
                    WHERE TABLE_SCHEMA = DATABASE()
                    AND TABLE_NAME = 'DesignTemplates'
                    AND INDEX_NAME = 'IX_DesignTemplates_CatalogStatus');
                SET @sqlStmt := IF(@exist > 0,
                    'DROP INDEX `IX_DesignTemplates_CatalogStatus` ON `DesignTemplates`',
                    'SELECT 1');
                PREPARE stmt FROM @sqlStmt;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            ");

            // Xóa cột CatalogStatus khỏi DesignTemplates (nếu tồn tại)
            migrationBuilder.Sql(@"
                SET @exist := (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = DATABASE()
                    AND TABLE_NAME = 'DesignTemplates'
                    AND COLUMN_NAME = 'CatalogStatus');
                SET @sqlStmt := IF(@exist > 0,
                    'ALTER TABLE `DesignTemplates` DROP COLUMN `CatalogStatus`',
                    'SELECT 1');
                PREPARE stmt FROM @sqlStmt;
                EXECUTE stmt;
                DEALLOCATE PREPARE stmt;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CatalogStatus",
                table: "DesignTemplates",
                type: "varchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "PUBLISHED")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_DesignTemplates_CatalogStatus",
                table: "DesignTemplates",
                column: "CatalogStatus");
        }
    }
}
