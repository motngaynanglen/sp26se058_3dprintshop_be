using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sp26se058_3dprintshop_be.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCarrierShippingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Shipment_Orders_OrderId",
                table: "Shipment");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_Deleted",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_TechnicalDrafts_Deleted",
                table: "TechnicalDrafts");

            migrationBuilder.DropIndex(
                name: "IX_Staffs_Deleted",
                table: "Staffs");

            migrationBuilder.DropIndex(
                name: "IX_ShippingAddress_Deleted",
                table: "ShippingAddress");

            migrationBuilder.DropIndex(
                name: "IX_Shipment_Deleted",
                table: "Shipment");

            migrationBuilder.DropIndex(
                name: "IX_ServiceSelections_Deleted",
                table: "ServiceSelections");

            migrationBuilder.DropIndex(
                name: "IX_ServiceSelectionOptions_Deleted",
                table: "ServiceSelectionOptions");

            migrationBuilder.DropIndex(
                name: "IX_ServicePackages_Deleted",
                table: "ServicePackages");

            migrationBuilder.DropIndex(
                name: "IX_ServiceOptions_Deleted",
                table: "ServiceOptions");

            migrationBuilder.DropIndex(
                name: "IX_PackageOptions_Deleted",
                table: "PackageOptions");

            migrationBuilder.DropIndex(
                name: "IX_Orders_Deleted",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_Deleted",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "IX_Materials_Deleted",
                table: "Materials");

            migrationBuilder.DropIndex(
                name: "IX_MaterialPriceHistories_Deleted",
                table: "MaterialPriceHistories");

            migrationBuilder.DropIndex(
                name: "IX_Managers_Deleted",
                table: "Managers");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_Deleted",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_InventoryTransactions_Deleted",
                table: "InventoryTransactions");

            migrationBuilder.DropIndex(
                name: "IX_Feedbacks_Deleted",
                table: "Feedbacks");

            migrationBuilder.DropIndex(
                name: "IX_DesignWorks_Deleted",
                table: "DesignWorks");

            migrationBuilder.DropIndex(
                name: "IX_DesignVersionHistorys_Deleted",
                table: "DesignVersionHistorys");

            migrationBuilder.DropIndex(
                name: "IX_DesignVariants_Deleted",
                table: "DesignVariants");

            migrationBuilder.DropIndex(
                name: "IX_DesignTemplates_Deleted",
                table: "DesignTemplates");

            migrationBuilder.DropIndex(
                name: "IX_DesignTags_Deleted",
                table: "DesignTags");

            migrationBuilder.DropIndex(
                name: "IX_DesignLogs_Deleted",
                table: "DesignLogs");

            migrationBuilder.DropIndex(
                name: "IX_Customers_Deleted",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_ConceptTags_Deleted",
                table: "ConceptTags");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_Deleted",
                table: "Accounts");

            migrationBuilder.AlterColumn<decimal>(
                name: "ShippingFee",
                table: "Shipment",
                type: "decimal(65,30)",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldPrecision: 18,
                oldScale: 2,
                oldDefaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Carrier",
                table: "Shipment",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CarrierLabelUrl",
                table: "Shipment",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CarrierMetaJson",
                table: "Shipment",
                type: "text",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CarrierOrderCode",
                table: "Shipment",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CarrierStatus",
                table: "Shipment",
                type: "varchar(80)",
                maxLength: 80,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_Deleted",
                table: "Transactions",
                column: "Deleted",
                filter: "`Deleted` IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalDrafts_Deleted",
                table: "TechnicalDrafts",
                column: "Deleted",
                filter: "`Deleted` IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Staffs_Deleted",
                table: "Staffs",
                column: "Deleted",
                filter: "`Deleted` IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingAddress_Deleted",
                table: "ShippingAddress",
                column: "Deleted",
                filter: "`Deleted` IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Shipment_Deleted",
                table: "Shipment",
                column: "Deleted",
                filter: "`Deleted` IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceSelections_Deleted",
                table: "ServiceSelections",
                column: "Deleted",
                filter: "`Deleted` IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceSelectionOptions_Deleted",
                table: "ServiceSelectionOptions",
                column: "Deleted",
                filter: "`Deleted` IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ServicePackages_Deleted",
                table: "ServicePackages",
                column: "Deleted",
                filter: "`Deleted` IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOptions_Deleted",
                table: "ServiceOptions",
                column: "Deleted",
                filter: "`Deleted` IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PackageOptions_Deleted",
                table: "PackageOptions",
                column: "Deleted",
                filter: "`Deleted` IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Deleted",
                table: "Orders",
                column: "Deleted",
                filter: "`Deleted` IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_Deleted",
                table: "OrderItems",
                column: "Deleted",
                filter: "`Deleted` IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Materials_Deleted",
                table: "Materials",
                column: "Deleted",
                filter: "`Deleted` IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialPriceHistories_Deleted",
                table: "MaterialPriceHistories",
                column: "Deleted",
                filter: "`Deleted` IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Managers_Deleted",
                table: "Managers",
                column: "Deleted",
                filter: "`Deleted` IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_Deleted",
                table: "Invoices",
                column: "Deleted",
                filter: "`Deleted` IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_Deleted",
                table: "InventoryTransactions",
                column: "Deleted",
                filter: "`Deleted` IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_Deleted",
                table: "Feedbacks",
                column: "Deleted",
                filter: "`Deleted` IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DesignWorks_Deleted",
                table: "DesignWorks",
                column: "Deleted",
                filter: "`Deleted` IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DesignVersionHistorys_Deleted",
                table: "DesignVersionHistorys",
                column: "Deleted",
                filter: "`Deleted` IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DesignVariants_Deleted",
                table: "DesignVariants",
                column: "Deleted",
                filter: "`Deleted` IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DesignTemplates_Deleted",
                table: "DesignTemplates",
                column: "Deleted",
                filter: "`Deleted` IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DesignTags_Deleted",
                table: "DesignTags",
                column: "Deleted",
                filter: "`Deleted` IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DesignLogs_Deleted",
                table: "DesignLogs",
                column: "Deleted",
                filter: "`Deleted` IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Deleted",
                table: "Customers",
                column: "Deleted",
                filter: "`Deleted` IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ConceptTags_Deleted",
                table: "ConceptTags",
                column: "Deleted",
                filter: "`Deleted` IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Deleted",
                table: "Accounts",
                column: "Deleted",
                filter: "`Deleted` IS NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Shipment_Orders_OrderId",
                table: "Shipment",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Shipment_Orders_OrderId",
                table: "Shipment");

            migrationBuilder.DropIndex(
                name: "IX_Transactions_Deleted",
                table: "Transactions");

            migrationBuilder.DropIndex(
                name: "IX_TechnicalDrafts_Deleted",
                table: "TechnicalDrafts");

            migrationBuilder.DropIndex(
                name: "IX_Staffs_Deleted",
                table: "Staffs");

            migrationBuilder.DropIndex(
                name: "IX_ShippingAddress_Deleted",
                table: "ShippingAddress");

            migrationBuilder.DropIndex(
                name: "IX_Shipment_Deleted",
                table: "Shipment");

            migrationBuilder.DropIndex(
                name: "IX_ServiceSelections_Deleted",
                table: "ServiceSelections");

            migrationBuilder.DropIndex(
                name: "IX_ServiceSelectionOptions_Deleted",
                table: "ServiceSelectionOptions");

            migrationBuilder.DropIndex(
                name: "IX_ServicePackages_Deleted",
                table: "ServicePackages");

            migrationBuilder.DropIndex(
                name: "IX_ServiceOptions_Deleted",
                table: "ServiceOptions");

            migrationBuilder.DropIndex(
                name: "IX_PackageOptions_Deleted",
                table: "PackageOptions");

            migrationBuilder.DropIndex(
                name: "IX_Orders_Deleted",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_OrderItems_Deleted",
                table: "OrderItems");

            migrationBuilder.DropIndex(
                name: "IX_Materials_Deleted",
                table: "Materials");

            migrationBuilder.DropIndex(
                name: "IX_MaterialPriceHistories_Deleted",
                table: "MaterialPriceHistories");

            migrationBuilder.DropIndex(
                name: "IX_Managers_Deleted",
                table: "Managers");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_Deleted",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_InventoryTransactions_Deleted",
                table: "InventoryTransactions");

            migrationBuilder.DropIndex(
                name: "IX_Feedbacks_Deleted",
                table: "Feedbacks");

            migrationBuilder.DropIndex(
                name: "IX_DesignWorks_Deleted",
                table: "DesignWorks");

            migrationBuilder.DropIndex(
                name: "IX_DesignVersionHistorys_Deleted",
                table: "DesignVersionHistorys");

            migrationBuilder.DropIndex(
                name: "IX_DesignVariants_Deleted",
                table: "DesignVariants");

            migrationBuilder.DropIndex(
                name: "IX_DesignTemplates_Deleted",
                table: "DesignTemplates");

            migrationBuilder.DropIndex(
                name: "IX_DesignTags_Deleted",
                table: "DesignTags");

            migrationBuilder.DropIndex(
                name: "IX_DesignLogs_Deleted",
                table: "DesignLogs");

            migrationBuilder.DropIndex(
                name: "IX_Customers_Deleted",
                table: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_ConceptTags_Deleted",
                table: "ConceptTags");

            migrationBuilder.DropIndex(
                name: "IX_Accounts_Deleted",
                table: "Accounts");

            migrationBuilder.DropColumn(
                name: "Carrier",
                table: "Shipment");

            migrationBuilder.DropColumn(
                name: "CarrierLabelUrl",
                table: "Shipment");

            migrationBuilder.DropColumn(
                name: "CarrierMetaJson",
                table: "Shipment");

            migrationBuilder.DropColumn(
                name: "CarrierOrderCode",
                table: "Shipment");

            migrationBuilder.DropColumn(
                name: "CarrierStatus",
                table: "Shipment");

            migrationBuilder.AlterColumn<decimal>(
                name: "ShippingFee",
                table: "Shipment",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(65,30)");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_Deleted",
                table: "Transactions",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_TechnicalDrafts_Deleted",
                table: "TechnicalDrafts",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_Staffs_Deleted",
                table: "Staffs",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingAddress_Deleted",
                table: "ShippingAddress",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_Shipment_Deleted",
                table: "Shipment",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceSelections_Deleted",
                table: "ServiceSelections",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceSelectionOptions_Deleted",
                table: "ServiceSelectionOptions",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_ServicePackages_Deleted",
                table: "ServicePackages",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceOptions_Deleted",
                table: "ServiceOptions",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_PackageOptions_Deleted",
                table: "PackageOptions",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_Deleted",
                table: "Orders",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_OrderItems_Deleted",
                table: "OrderItems",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_Materials_Deleted",
                table: "Materials",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_MaterialPriceHistories_Deleted",
                table: "MaterialPriceHistories",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_Managers_Deleted",
                table: "Managers",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_Deleted",
                table: "Invoices",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_Deleted",
                table: "InventoryTransactions",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_Feedbacks_Deleted",
                table: "Feedbacks",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_DesignWorks_Deleted",
                table: "DesignWorks",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_DesignVersionHistorys_Deleted",
                table: "DesignVersionHistorys",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_DesignVariants_Deleted",
                table: "DesignVariants",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_DesignTemplates_Deleted",
                table: "DesignTemplates",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_DesignTags_Deleted",
                table: "DesignTags",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_DesignLogs_Deleted",
                table: "DesignLogs",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_Deleted",
                table: "Customers",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_ConceptTags_Deleted",
                table: "ConceptTags",
                column: "Deleted");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Deleted",
                table: "Accounts",
                column: "Deleted");

            migrationBuilder.AddForeignKey(
                name: "FK_Shipment_Orders_OrderId",
                table: "Shipment",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
