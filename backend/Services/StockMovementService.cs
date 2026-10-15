using Microsoft.EntityFrameworkCore;
using ProjectBee.Data;
using ProjectBee.Models;

namespace ProjectBee.Services;

public class StockMovementService
{
    private readonly AppDbContext _db;

    public StockMovementService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<int> RegisterEntryAsync(
        Guid productId, Guid storageId, string batch, int quantity, string user)
    {
        _db.StockMovements.Add(new StockMovement
        {
            Type = MovementType.Entry,
            ProductId = productId,
            StorageId = storageId,
            Batch = batch,
            Quantity = quantity,
            CreatedBy = user
        });

        var balance = await _db.StockBalances.FindAsync(productId, storageId, batch);

        if (balance == null)
        {
            balance = new StockBalance
            {
                ProductId = productId,
                StorageId = storageId,
                Batch = batch,
                Balance = 0
            };
            _db.StockBalances.Add(balance);
        }

        balance.Balance += quantity;

        await _db.SaveChangesAsync();

        return balance.Balance;
    }
}