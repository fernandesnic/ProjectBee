using Microsoft.EntityFrameworkCore;
using ProjectBee.Data;
using ProjectBee.Models;

namespace ProjectBee.Services;

public record MovementResult(bool Success, int Balance, string? Error = null)
{
    public static MovementResult Ok(int balance) => new(true, balance);
    public static MovementResult Fail(string error) => new(false, 0, error);
}

public class StockMovementService
{
    private readonly AppDbContext _db;

    public StockMovementService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<MovementResult> RegisterEntryAsync(
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

        var balance = await GetOrCreateBalanceAsync(productId, storageId, batch);
        balance.Balance += quantity;

        await _db.SaveChangesAsync();

        return MovementResult.Ok(balance.Balance);
    }

    public async Task<MovementResult> RegisterExitAsync(
        Guid productId, Guid storageId, string batch, int quantity, string user)
    {
        var balance = await _db.StockBalances.FindAsync(productId, storageId, batch);

        if (balance == null)
            return MovementResult.Fail("Não existe saldo para este produto, armazém e lote.");

        if (balance.Balance < quantity)
            return MovementResult.Fail($"Saldo insuficiente. Disponível: {balance.Balance}, solicitado: {quantity}.");

        _db.StockMovements.Add(new StockMovement
        {
            Type = MovementType.Exit,
            ProductId = productId,
            StorageId = storageId,
            Batch = batch,
            Quantity = -quantity,   
            CreatedBy = user
        });

        balance.Balance -= quantity;

        await _db.SaveChangesAsync();

        return MovementResult.Ok(balance.Balance);
    }

    public async Task<MovementResult> RegisterTransferAsync(
        Guid productId, Guid fromStorageId, Guid toStorageId, string batch, int quantity, string user)
    {
        if (fromStorageId == toStorageId)
            return MovementResult.Fail("O armazém de origem e o de destino devem ser diferentes.");

        var source = await _db.StockBalances.FindAsync(productId, fromStorageId, batch);

        if (source == null)
            return MovementResult.Fail("Não existe saldo deste lote no armazém de origem.");

        if (source.Balance < quantity)
            return MovementResult.Fail($"Saldo insuficiente na origem. Disponível: {source.Balance}, solicitado: {quantity}.");

        var destination = await GetOrCreateBalanceAsync(productId, toStorageId, batch);
        var transferId = Guid.NewGuid();

        _db.StockMovements.Add(new StockMovement
        {
            Type = MovementType.TransferOut,   
            ProductId = productId,
            StorageId = fromStorageId,
            Batch = batch,
            Quantity = -quantity,
            TransferId = transferId,
            CreatedBy = user
        });

        _db.StockMovements.Add(new StockMovement
        {
            Type = MovementType.TransferIn,    
            ProductId = productId,
            StorageId = toStorageId,
            Batch = batch,
            Quantity = quantity,
            TransferId = transferId,
            CreatedBy = user
        });

        source.Balance -= quantity;
        destination.Balance += quantity;

        await _db.SaveChangesAsync();   

        return MovementResult.Ok(source.Balance);
    }

    public async Task<MovementResult> RegisterAdjustmentAsync(
    Guid productId, Guid storageId, string batch, int countedQuantity, string reason, string user)
    {
        var balance = await GetOrCreateBalanceAsync(productId, storageId, batch);
        var difference = countedQuantity - balance.Balance;

        if (difference == 0)
            return MovementResult.Fail($"Não há diferença para ajustar. Saldo atual já é {balance.Balance}.");

        _db.StockMovements.Add(new StockMovement
        {
            Type = MovementType.Adjustment,
            ProductId = productId,
            StorageId = storageId,
            Batch = batch,
            Quantity = difference,   // + sobrou, - faltou
            Reason = reason,
            CreatedBy = user
        });

        balance.Balance = countedQuantity;

        await _db.SaveChangesAsync();

        return MovementResult.Ok(balance.Balance);
    }

    private async Task<StockBalance> GetOrCreateBalanceAsync(Guid productId, Guid storageId, string batch)
    {
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

        return balance;
    }
}