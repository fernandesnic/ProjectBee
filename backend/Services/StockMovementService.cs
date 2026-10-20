using Microsoft.EntityFrameworkCore;
using ProjectBee.Data;
using ProjectBee.Models;

namespace ProjectBee.Services;

public record MovementResult(bool Success, int Balance, string? Error = null, bool IsConflict = false)
{
    public static MovementResult Ok(int balance) => new(true, balance);
    public static MovementResult Fail(string error) => new(false, 0, error);
    public static MovementResult Conflict() => new(false, 0,
        "O saldo foi alterado por outra operação ao mesmo tempo. Consulte o saldo atualizado e tente novamente.",
        IsConflict: true);
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
        var balance = await StageEntryAsync(productId, storageId, batch, quantity, user);

        if (!await TrySaveAsync())
            return MovementResult.Conflict();

        return MovementResult.Ok(balance.Balance);
    }

    // Lança movimento e saldo no contexto sem gravar: quem chama decide quando salvar, e em qual
    // transação (o recebimento de compra grava a nota, os itens e as entradas juntos).
    public async Task<StockBalance> StageEntryAsync(
        Guid productId, Guid storageId, string batch, int quantity, string user,
        MovementType type = MovementType.Entry, Guid? purchaseReceiptId = null)
    {
        _db.StockMovements.Add(new StockMovement
        {
            Type = type,
            ProductId = productId,
            StorageId = storageId,
            Batch = batch,
            Quantity = quantity,
            PurchaseReceiptId = purchaseReceiptId,
            CreatedBy = user
        });

        var balance = await GetOrCreateBalanceAsync(productId, storageId, batch);
        balance.Balance += quantity;
        return balance;
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

        if (!await TrySaveAsync())
            return MovementResult.Conflict();

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

        if (!await TrySaveAsync())
            return MovementResult.Conflict();

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

        if (!await TrySaveAsync())
            return MovementResult.Conflict();

        return MovementResult.Ok(balance.Balance);
    }

    // O RowVersion do saldo vai no WHERE do UPDATE. Se outra requisição gravou depois da nossa
    // leitura, nenhuma linha é afetada e nada é salvo: nem o saldo, nem o movimento.
    private async Task<bool> TrySaveAsync()
    {
        try
        {
            await _db.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
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