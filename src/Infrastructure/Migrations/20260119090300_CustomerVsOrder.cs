using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sp26se058_3dprintshop_be.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CustomerVsOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DesignTag_ConceptTag_ConceptTagId",
                table: "DesignTag");

            migrationBuilder.DropForeignKey(
                name: "FK_DesignTag_DesignTemplate_DesignTemplateId",
                table: "DesignTag");

            migrationBuilder.DropForeignKey(
                name: "FK_DesignVariant_DesignTemplate_DesignTemplateId",
                table: "DesignVariant");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoice_Order_OrderId",
                table: "Invoice");

            migrationBuilder.DropForeignKey(
                name: "FK_MaterialPriceHistory_Material_MaterialId",
                table: "MaterialPriceHistory");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderItem_DesignVariant_DesignVariantId",
                table: "OrderItem");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderItem_Material_MaterialId",
                table: "OrderItem");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderItem_Order_OrderId",
                table: "OrderItem");

            migrationBuilder.DropForeignKey(
                name: "FK_Transaction_Invoice_InvoiceId",
                table: "Transaction");

            migrationBuilder.DropForeignKey(
                name: "FK_VariantMaterialOption_DesignVariant_DesignVariantId",
                table: "VariantMaterialOption");

            migrationBuilder.DropForeignKey(
                name: "FK_VariantMaterialOption_Material_MaterialId",
                table: "VariantMaterialOption");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VariantMaterialOption",
                table: "VariantMaterialOption");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Transaction",
                table: "Transaction");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OrderItem",
                table: "OrderItem");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Order",
                table: "Order");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MaterialPriceHistory",
                table: "MaterialPriceHistory");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Material",
                table: "Material");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Invoice",
                table: "Invoice");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DesignVariant",
                table: "DesignVariant");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DesignTemplate",
                table: "DesignTemplate");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DesignTag",
                table: "DesignTag");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ConceptTag",
                table: "ConceptTag");

            migrationBuilder.RenameTable(
                name: "VariantMaterialOption",
                newName: "VariantMaterialOptions");

            migrationBuilder.RenameTable(
                name: "Transaction",
                newName: "Transactions");

            migrationBuilder.RenameTable(
                name: "OrderItem",
                newName: "OrderItems");

            migrationBuilder.RenameTable(
                name: "Order",
                newName: "Orders");

            migrationBuilder.RenameTable(
                name: "MaterialPriceHistory",
                newName: "MaterialPriceHistories");

            migrationBuilder.RenameTable(
                name: "Material",
                newName: "Materials");

            migrationBuilder.RenameTable(
                name: "Invoice",
                newName: "Invoices");

            migrationBuilder.RenameTable(
                name: "DesignVariant",
                newName: "DesignVariants");

            migrationBuilder.RenameTable(
                name: "DesignTemplate",
                newName: "DesignTemplates");

            migrationBuilder.RenameTable(
                name: "DesignTag",
                newName: "DesignTags");

            migrationBuilder.RenameTable(
                name: "ConceptTag",
                newName: "ConceptTags");

            migrationBuilder.RenameIndex(
                name: "IX_VariantMaterialOption_MaterialId",
                table: "VariantMaterialOptions",
                newName: "IX_VariantMaterialOptions_MaterialId");

            migrationBuilder.RenameIndex(
                name: "IX_VariantMaterialOption_DesignVariantId",
                table: "VariantMaterialOptions",
                newName: "IX_VariantMaterialOptions_DesignVariantId");

            migrationBuilder.RenameIndex(
                name: "IX_Transaction_InvoiceId",
                table: "Transactions",
                newName: "IX_Transactions_InvoiceId");

            migrationBuilder.RenameIndex(
                name: "IX_Transaction_ExternalTransactionId",
                table: "Transactions",
                newName: "IX_Transactions_ExternalTransactionId");

            migrationBuilder.RenameIndex(
                name: "IX_OrderItem_OrderId",
                table: "OrderItems",
                newName: "IX_OrderItems_OrderId");

            migrationBuilder.RenameIndex(
                name: "IX_OrderItem_MaterialId",
                table: "OrderItems",
                newName: "IX_OrderItems_MaterialId");

            migrationBuilder.RenameIndex(
                name: "IX_OrderItem_DesignVariantId",
                table: "OrderItems",
                newName: "IX_OrderItems_DesignVariantId");

            migrationBuilder.RenameIndex(
                name: "IX_MaterialPriceHistory_MaterialId",
                table: "MaterialPriceHistories",
                newName: "IX_MaterialPriceHistories_MaterialId");

            migrationBuilder.RenameIndex(
                name: "IX_Invoice_OrderId",
                table: "Invoices",
                newName: "IX_Invoices_OrderId");

            migrationBuilder.RenameIndex(
                name: "IX_DesignVariant_DesignTemplateId",
                table: "DesignVariants",
                newName: "IX_DesignVariants_DesignTemplateId");

            migrationBuilder.RenameIndex(
                name: "IX_DesignTag_DesignTemplateId",
                table: "DesignTags",
                newName: "IX_DesignTags_DesignTemplateId");

            migrationBuilder.RenameIndex(
                name: "IX_DesignTag_ConceptTagId",
                table: "DesignTags",
                newName: "IX_DesignTags_ConceptTagId");

            migrationBuilder.AlterColumn<string>(
                name: "OrderStatus",
                table: "Orders",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "PENDING",
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AddPrimaryKey(
                name: "PK_VariantMaterialOptions",
                table: "VariantMaterialOptions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Transactions",
                table: "Transactions",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrderItems",
                table: "OrderItems",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Orders",
                table: "Orders",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MaterialPriceHistories",
                table: "MaterialPriceHistories",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Materials",
                table: "Materials",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Invoices",
                table: "Invoices",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DesignVariants",
                table: "DesignVariants",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DesignTemplates",
                table: "DesignTemplates",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DesignTags",
                table: "DesignTags",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ConceptTags",
                table: "ConceptTags",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerId",
                table: "Orders",
                column: "CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_DesignTags_ConceptTags_ConceptTagId",
                table: "DesignTags",
                column: "ConceptTagId",
                principalTable: "ConceptTags",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DesignTags_DesignTemplates_DesignTemplateId",
                table: "DesignTags",
                column: "DesignTemplateId",
                principalTable: "DesignTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DesignVariants_DesignTemplates_DesignTemplateId",
                table: "DesignVariants",
                column: "DesignTemplateId",
                principalTable: "DesignTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Orders_OrderId",
                table: "Invoices",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialPriceHistories_Materials_MaterialId",
                table: "MaterialPriceHistories",
                column: "MaterialId",
                principalTable: "Materials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_DesignVariants_DesignVariantId",
                table: "OrderItems",
                column: "DesignVariantId",
                principalTable: "DesignVariants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_Materials_MaterialId",
                table: "OrderItems",
                column: "MaterialId",
                principalTable: "Materials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItems_Orders_OrderId",
                table: "OrderItems",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Orders_Customers_CustomerId",
                table: "Orders",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Transactions_Invoices_InvoiceId",
                table: "Transactions",
                column: "InvoiceId",
                principalTable: "Invoices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VariantMaterialOptions_DesignVariants_DesignVariantId",
                table: "VariantMaterialOptions",
                column: "DesignVariantId",
                principalTable: "DesignVariants",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VariantMaterialOptions_Materials_MaterialId",
                table: "VariantMaterialOptions",
                column: "MaterialId",
                principalTable: "Materials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DesignTags_ConceptTags_ConceptTagId",
                table: "DesignTags");

            migrationBuilder.DropForeignKey(
                name: "FK_DesignTags_DesignTemplates_DesignTemplateId",
                table: "DesignTags");

            migrationBuilder.DropForeignKey(
                name: "FK_DesignVariants_DesignTemplates_DesignTemplateId",
                table: "DesignVariants");

            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Orders_OrderId",
                table: "Invoices");

            migrationBuilder.DropForeignKey(
                name: "FK_MaterialPriceHistories_Materials_MaterialId",
                table: "MaterialPriceHistories");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_DesignVariants_DesignVariantId",
                table: "OrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_Materials_MaterialId",
                table: "OrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_OrderItems_Orders_OrderId",
                table: "OrderItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Orders_Customers_CustomerId",
                table: "Orders");

            migrationBuilder.DropForeignKey(
                name: "FK_Transactions_Invoices_InvoiceId",
                table: "Transactions");

            migrationBuilder.DropForeignKey(
                name: "FK_VariantMaterialOptions_DesignVariants_DesignVariantId",
                table: "VariantMaterialOptions");

            migrationBuilder.DropForeignKey(
                name: "FK_VariantMaterialOptions_Materials_MaterialId",
                table: "VariantMaterialOptions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_VariantMaterialOptions",
                table: "VariantMaterialOptions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Transactions",
                table: "Transactions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Orders",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_CustomerId",
                table: "Orders");

            migrationBuilder.DropPrimaryKey(
                name: "PK_OrderItems",
                table: "OrderItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Materials",
                table: "Materials");

            migrationBuilder.DropPrimaryKey(
                name: "PK_MaterialPriceHistories",
                table: "MaterialPriceHistories");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Invoices",
                table: "Invoices");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DesignVariants",
                table: "DesignVariants");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DesignTemplates",
                table: "DesignTemplates");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DesignTags",
                table: "DesignTags");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ConceptTags",
                table: "ConceptTags");

            migrationBuilder.RenameTable(
                name: "VariantMaterialOptions",
                newName: "VariantMaterialOption");

            migrationBuilder.RenameTable(
                name: "Transactions",
                newName: "Transaction");

            migrationBuilder.RenameTable(
                name: "Orders",
                newName: "Order");

            migrationBuilder.RenameTable(
                name: "OrderItems",
                newName: "OrderItem");

            migrationBuilder.RenameTable(
                name: "Materials",
                newName: "Material");

            migrationBuilder.RenameTable(
                name: "MaterialPriceHistories",
                newName: "MaterialPriceHistory");

            migrationBuilder.RenameTable(
                name: "Invoices",
                newName: "Invoice");

            migrationBuilder.RenameTable(
                name: "DesignVariants",
                newName: "DesignVariant");

            migrationBuilder.RenameTable(
                name: "DesignTemplates",
                newName: "DesignTemplate");

            migrationBuilder.RenameTable(
                name: "DesignTags",
                newName: "DesignTag");

            migrationBuilder.RenameTable(
                name: "ConceptTags",
                newName: "ConceptTag");

            migrationBuilder.RenameIndex(
                name: "IX_VariantMaterialOptions_MaterialId",
                table: "VariantMaterialOption",
                newName: "IX_VariantMaterialOption_MaterialId");

            migrationBuilder.RenameIndex(
                name: "IX_VariantMaterialOptions_DesignVariantId",
                table: "VariantMaterialOption",
                newName: "IX_VariantMaterialOption_DesignVariantId");

            migrationBuilder.RenameIndex(
                name: "IX_Transactions_InvoiceId",
                table: "Transaction",
                newName: "IX_Transaction_InvoiceId");

            migrationBuilder.RenameIndex(
                name: "IX_Transactions_ExternalTransactionId",
                table: "Transaction",
                newName: "IX_Transaction_ExternalTransactionId");

            migrationBuilder.RenameIndex(
                name: "IX_OrderItems_OrderId",
                table: "OrderItem",
                newName: "IX_OrderItem_OrderId");

            migrationBuilder.RenameIndex(
                name: "IX_OrderItems_MaterialId",
                table: "OrderItem",
                newName: "IX_OrderItem_MaterialId");

            migrationBuilder.RenameIndex(
                name: "IX_OrderItems_DesignVariantId",
                table: "OrderItem",
                newName: "IX_OrderItem_DesignVariantId");

            migrationBuilder.RenameIndex(
                name: "IX_MaterialPriceHistories_MaterialId",
                table: "MaterialPriceHistory",
                newName: "IX_MaterialPriceHistory_MaterialId");

            migrationBuilder.RenameIndex(
                name: "IX_Invoices_OrderId",
                table: "Invoice",
                newName: "IX_Invoice_OrderId");

            migrationBuilder.RenameIndex(
                name: "IX_DesignVariants_DesignTemplateId",
                table: "DesignVariant",
                newName: "IX_DesignVariant_DesignTemplateId");

            migrationBuilder.RenameIndex(
                name: "IX_DesignTags_DesignTemplateId",
                table: "DesignTag",
                newName: "IX_DesignTag_DesignTemplateId");

            migrationBuilder.RenameIndex(
                name: "IX_DesignTags_ConceptTagId",
                table: "DesignTag",
                newName: "IX_DesignTag_ConceptTagId");

            migrationBuilder.AlterColumn<string>(
                name: "OrderStatus",
                table: "Order",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "PENDING");

            migrationBuilder.AddPrimaryKey(
                name: "PK_VariantMaterialOption",
                table: "VariantMaterialOption",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Transaction",
                table: "Transaction",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Order",
                table: "Order",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_OrderItem",
                table: "OrderItem",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Material",
                table: "Material",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_MaterialPriceHistory",
                table: "MaterialPriceHistory",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Invoice",
                table: "Invoice",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DesignVariant",
                table: "DesignVariant",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DesignTemplate",
                table: "DesignTemplate",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DesignTag",
                table: "DesignTag",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ConceptTag",
                table: "ConceptTag",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DesignTag_ConceptTag_ConceptTagId",
                table: "DesignTag",
                column: "ConceptTagId",
                principalTable: "ConceptTag",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DesignTag_DesignTemplate_DesignTemplateId",
                table: "DesignTag",
                column: "DesignTemplateId",
                principalTable: "DesignTemplate",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DesignVariant_DesignTemplate_DesignTemplateId",
                table: "DesignVariant",
                column: "DesignTemplateId",
                principalTable: "DesignTemplate",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Invoice_Order_OrderId",
                table: "Invoice",
                column: "OrderId",
                principalTable: "Order",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_MaterialPriceHistory_Material_MaterialId",
                table: "MaterialPriceHistory",
                column: "MaterialId",
                principalTable: "Material",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItem_DesignVariant_DesignVariantId",
                table: "OrderItem",
                column: "DesignVariantId",
                principalTable: "DesignVariant",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItem_Material_MaterialId",
                table: "OrderItem",
                column: "MaterialId",
                principalTable: "Material",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_OrderItem_Order_OrderId",
                table: "OrderItem",
                column: "OrderId",
                principalTable: "Order",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Transaction_Invoice_InvoiceId",
                table: "Transaction",
                column: "InvoiceId",
                principalTable: "Invoice",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VariantMaterialOption_DesignVariant_DesignVariantId",
                table: "VariantMaterialOption",
                column: "DesignVariantId",
                principalTable: "DesignVariant",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_VariantMaterialOption_Material_MaterialId",
                table: "VariantMaterialOption",
                column: "MaterialId",
                principalTable: "Material",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
