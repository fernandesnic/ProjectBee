using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProjectBee.Migrations
{
    /// <inheritdoc />
    public partial class AddOpeningBalanceMovements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO StockMovements
                    (Id, Type, ProductId, StorageId, Batch, Quantity, Reason, TransferId, CreatedBy, CreatedAt)
                SELECT
                    NEWID(), 6, b.ProductId, b.StorageId, b.Batch,
                    b.Balance - ISNULL(m.Total, 0),
                    'Saldo inicial (implantação do livro-razão)',
                    NULL, 'system', SYSUTCDATETIME()
                FROM StockBalances b
                LEFT JOIN (
                    SELECT ProductId, StorageId, Batch, SUM(Quantity) AS Total
                    FROM StockMovements
                    GROUP BY ProductId, StorageId, Batch
                ) m ON m.ProductId = b.ProductId
                   AND m.StorageId = b.StorageId
                   AND m.Batch = b.Batch
                WHERE b.Balance - ISNULL(m.Total, 0) <> 0;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM StockMovements WHERE Type = 6;");
        }
    }
}
