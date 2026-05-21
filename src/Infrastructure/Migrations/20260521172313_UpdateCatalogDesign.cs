using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sp26se058_3dprintshop_be.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCatalogDesign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            AddColumnIfNotExists(
                migrationBuilder,
                "DesignVariants",
                "CatalogStatus",
                "ALTER TABLE `DesignVariants` ADD `CatalogStatus` varchar(30) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'PUBLISHED'");

            AddColumnIfNotExists(
                migrationBuilder,
                "DesignVariants",
                "ImageUrls",
                "ALTER TABLE `DesignVariants` ADD `ImageUrls` longtext NULL");

            AddColumnIfNotExists(
                migrationBuilder,
                "DesignTemplates",
                "CatalogStatus",
                "ALTER TABLE `DesignTemplates` ADD `CatalogStatus` varchar(30) CHARACTER SET utf8mb4 NOT NULL DEFAULT 'PUBLISHED'");

            CreateIndexIfNotExists(
                migrationBuilder,
                "DesignVariants",
                "IX_DesignVariants_CatalogStatus",
                "CREATE INDEX `IX_DesignVariants_CatalogStatus` ON `DesignVariants` (`CatalogStatus`)");

            CreateIndexIfNotExists(
                migrationBuilder,
                "DesignTemplates",
                "IX_DesignTemplates_CatalogStatus",
                "CREATE INDEX `IX_DesignTemplates_CatalogStatus` ON `DesignTemplates` (`CatalogStatus`)");

            CreateIndexIfNotExists(
                migrationBuilder,
                "DesignTags",
                "IX_DesignTags_DesignTemplateId_ConceptTagId",
                "CREATE INDEX `IX_DesignTags_DesignTemplateId_ConceptTagId` ON `DesignTags` (`DesignTemplateId`, `ConceptTagId`)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            DropIndexIfExists(
                migrationBuilder,
                "DesignVariants",
                "IX_DesignVariants_CatalogStatus",
                "DROP INDEX `IX_DesignVariants_CatalogStatus` ON `DesignVariants`");

            DropIndexIfExists(
                migrationBuilder,
                "DesignTemplates",
                "IX_DesignTemplates_CatalogStatus",
                "DROP INDEX `IX_DesignTemplates_CatalogStatus` ON `DesignTemplates`");

            DropIndexIfExists(
                migrationBuilder,
                "DesignTags",
                "IX_DesignTags_DesignTemplateId_ConceptTagId",
                "DROP INDEX `IX_DesignTags_DesignTemplateId_ConceptTagId` ON `DesignTags`");

            DropColumnIfExists(
                migrationBuilder,
                "DesignVariants",
                "CatalogStatus",
                "ALTER TABLE `DesignVariants` DROP COLUMN `CatalogStatus`");

            DropColumnIfExists(
                migrationBuilder,
                "DesignVariants",
                "ImageUrls",
                "ALTER TABLE `DesignVariants` DROP COLUMN `ImageUrls`");

            DropColumnIfExists(
                migrationBuilder,
                "DesignTemplates",
                "CatalogStatus",
                "ALTER TABLE `DesignTemplates` DROP COLUMN `CatalogStatus`");
        }

        private static void AddColumnIfNotExists(MigrationBuilder migrationBuilder, string tableName, string columnName, string alterSql)
        {
            ExecuteIf(
                migrationBuilder,
                $@"SELECT COUNT(*)
                   FROM INFORMATION_SCHEMA.COLUMNS
                   WHERE TABLE_SCHEMA = DATABASE()
                     AND TABLE_NAME = '{tableName}'
                     AND COLUMN_NAME = '{columnName}'",
                0,
                alterSql);
        }

        private static void DropColumnIfExists(MigrationBuilder migrationBuilder, string tableName, string columnName, string alterSql)
        {
            ExecuteIf(
                migrationBuilder,
                $@"SELECT COUNT(*)
                   FROM INFORMATION_SCHEMA.COLUMNS
                   WHERE TABLE_SCHEMA = DATABASE()
                     AND TABLE_NAME = '{tableName}'
                     AND COLUMN_NAME = '{columnName}'",
                1,
                alterSql);
        }

        private static void CreateIndexIfNotExists(MigrationBuilder migrationBuilder, string tableName, string indexName, string createSql)
        {
            ExecuteIf(
                migrationBuilder,
                $@"SELECT COUNT(*)
                   FROM INFORMATION_SCHEMA.STATISTICS
                   WHERE TABLE_SCHEMA = DATABASE()
                     AND TABLE_NAME = '{tableName}'
                     AND INDEX_NAME = '{indexName}'",
                0,
                createSql);
        }

        private static void DropIndexIfExists(MigrationBuilder migrationBuilder, string tableName, string indexName, string dropSql)
        {
            ExecuteIf(
                migrationBuilder,
                $@"SELECT COUNT(*)
                   FROM INFORMATION_SCHEMA.STATISTICS
                   WHERE TABLE_SCHEMA = DATABASE()
                     AND TABLE_NAME = '{tableName}'
                     AND INDEX_NAME = '{indexName}'",
                1,
                dropSql);
        }

        private static void ExecuteIf(MigrationBuilder migrationBuilder, string countSql, int expectedCount, string commandSql)
        {
            var escapedCommandSql = commandSql.Replace("'", "''");

            migrationBuilder.Sql($@"SET @migration_sql = IF(({countSql}) = {expectedCount}, '{escapedCommandSql}', 'SELECT 1')");
            migrationBuilder.Sql("PREPARE migration_stmt FROM @migration_sql");
            migrationBuilder.Sql("EXECUTE migration_stmt");
            migrationBuilder.Sql("DEALLOCATE PREPARE migration_stmt");
        }
    }
}
