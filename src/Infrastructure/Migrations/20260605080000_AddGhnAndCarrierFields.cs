using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sp26se058_3dprintshop_be.Infrastructure.Migrations
{
    public partial class AddGhnAndCarrierFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ShippingAddress — GHN fields
            migrationBuilder.Sql(@"
                SET @exist := (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'ShippingAddresses' AND COLUMN_NAME = 'GhnDistrictId');
                SET @sql := IF(@exist = 0, 'ALTER TABLE `ShippingAddresses` ADD `GhnDistrictId` int NULL', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
            ");

            migrationBuilder.Sql(@"
                SET @exist := (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'ShippingAddresses' AND COLUMN_NAME = 'GhnWardCode');
                SET @sql := IF(@exist = 0, 'ALTER TABLE `ShippingAddresses` ADD `GhnWardCode` longtext NULL', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
            ");

            // Shipment — Carrier fields
            migrationBuilder.Sql(@"
                SET @exist := (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Shipments' AND COLUMN_NAME = 'Carrier');
                SET @sql := IF(@exist = 0, 'ALTER TABLE `Shipments` ADD `Carrier` longtext NULL', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
            ");

            migrationBuilder.Sql(@"
                SET @exist := (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Shipments' AND COLUMN_NAME = 'CarrierOrderCode');
                SET @sql := IF(@exist = 0, 'ALTER TABLE `Shipments` ADD `CarrierOrderCode` longtext NULL', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
            ");

            migrationBuilder.Sql(@"
                SET @exist := (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Shipments' AND COLUMN_NAME = 'CarrierStatus');
                SET @sql := IF(@exist = 0, 'ALTER TABLE `Shipments` ADD `CarrierStatus` longtext NULL', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
            ");

            migrationBuilder.Sql(@"
                SET @exist := (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Shipments' AND COLUMN_NAME = 'CarrierLabelUrl');
                SET @sql := IF(@exist = 0, 'ALTER TABLE `Shipments` ADD `CarrierLabelUrl` longtext NULL', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
            ");

            migrationBuilder.Sql(@"
                SET @exist := (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
                    WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Shipments' AND COLUMN_NAME = 'CarrierMetaJson');
                SET @sql := IF(@exist = 0, 'ALTER TABLE `Shipments` ADD `CarrierMetaJson` longtext NULL', 'SELECT 1');
                PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "GhnDistrictId", table: "ShippingAddresses");
            migrationBuilder.DropColumn(name: "GhnWardCode", table: "ShippingAddresses");
            migrationBuilder.DropColumn(name: "Carrier", table: "Shipments");
            migrationBuilder.DropColumn(name: "CarrierOrderCode", table: "Shipments");
            migrationBuilder.DropColumn(name: "CarrierStatus", table: "Shipments");
            migrationBuilder.DropColumn(name: "CarrierLabelUrl", table: "Shipments");
            migrationBuilder.DropColumn(name: "CarrierMetaJson", table: "Shipments");
        }
    }
}
