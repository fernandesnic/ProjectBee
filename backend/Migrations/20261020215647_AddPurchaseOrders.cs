using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectBee.Migrations
{
    /// <inheritdoc />
    public partial class AddPurchaseOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PurchaseReceiptId",
                table: "StockMovements",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PurchaseOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SupplierId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StorageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CancelledBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelReason = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Storages_StorageId",
                        column: x => x.StorageId,
                        principalTable: "Storages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrders_Suppliers_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "Suppliers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseOrderItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false),
                    ReceivedQuantity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseOrderItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItems_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseOrderItems_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseReceipts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseOrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PriceDivergenceReason = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseReceipts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseReceipts_PurchaseOrders_PurchaseOrderId",
                        column: x => x.PurchaseOrderId,
                        principalTable: "PurchaseOrders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PurchaseReceiptItem",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseReceiptId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PurchaseOrderItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Batch = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(14,2)", precision: 14, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PurchaseReceiptItem", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PurchaseReceiptItem_PurchaseOrderItems_PurchaseOrderItemId",
                        column: x => x.PurchaseOrderItemId,
                        principalTable: "PurchaseOrderItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PurchaseReceiptItem_PurchaseReceipts_PurchaseReceiptId",
                        column: x => x.PurchaseReceiptId,
                        principalTable: "PurchaseReceipts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_PurchaseReceiptId",
                table: "StockMovements",
                column: "PurchaseReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItems_ProductId",
                table: "PurchaseOrderItems",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrderItems_PurchaseOrderId_ProductId",
                table: "PurchaseOrderItems",
                columns: new[] { "PurchaseOrderId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_Number",
                table: "PurchaseOrders",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_Status",
                table: "PurchaseOrders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_StorageId",
                table: "PurchaseOrders",
                column: "StorageId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseOrders_SupplierId",
                table: "PurchaseOrders",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReceiptItem_PurchaseOrderItemId",
                table: "PurchaseReceiptItem",
                column: "PurchaseOrderItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReceiptItem_PurchaseReceiptId",
                table: "PurchaseReceiptItem",
                column: "PurchaseReceiptId");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReceipts_InvoiceNumber",
                table: "PurchaseReceipts",
                column: "InvoiceNumber");

            migrationBuilder.CreateIndex(
                name: "IX_PurchaseReceipts_PurchaseOrderId",
                table: "PurchaseReceipts",
                column: "PurchaseOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_PurchaseReceipts_PurchaseReceiptId",
                table: "StockMovements",
                column: "PurchaseReceiptId",
                principalTable: "PurchaseReceipts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_PurchaseReceipts_PurchaseReceiptId",
                table: "StockMovements");

            migrationBuilder.DropTable(
                name: "PurchaseReceiptItem");

            migrationBuilder.DropTable(
                name: "PurchaseOrderItems");

            migrationBuilder.DropTable(
                name: "PurchaseReceipts");

            migrationBuilder.DropTable(
                name: "PurchaseOrders");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_PurchaseReceiptId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "PurchaseReceiptId",
                table: "StockMovements");
        }
    }
}
