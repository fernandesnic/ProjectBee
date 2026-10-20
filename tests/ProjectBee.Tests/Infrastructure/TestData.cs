using Microsoft.EntityFrameworkCore;
using ProjectBee.Data;
using ProjectBee.Models;

namespace ProjectBee.Tests.Infrastructure;

// Cada teste cria o próprio produto e armazém, para não depender da ordem nem de dados de outro teste.
public static class TestData
{
    public const string Batch = "LOTE-TESTE";

    public static async Task<Product> CreateProductAsync(TestDatabase database)
    {
        var product = new Product
        {
            Name = "Produto de teste",
            SKU = $"T{Guid.NewGuid():N}"[..15].ToUpperInvariant(),
            Desc = "Criado pelos testes",
            Price = 10m,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await using var db = database.CreateContext();
        db.Products.Add(product);
        await db.SaveChangesAsync();
        return product;
    }

    public static async Task<Storage> CreateStorageAsync(TestDatabase database)
    {
        var storage = new Storage
        {
            Name = "Armazém de teste",
            IdNumber = $"A{Guid.NewGuid():N}"[..10],
            AddressStreet = "Rua dos Testes",
            AddressNumber = "100",
            AddressCity = "São Paulo",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await using var db = database.CreateContext();
        db.Storages.Add(storage);
        await db.SaveChangesAsync();
        return storage;
    }

    public static async Task<Supplier> CreateSupplierAsync(TestDatabase database)
    {
        var supplier = new Supplier
        {
            Name = "Fornecedor de teste",
            Document = Random.Shared.NextInt64(10_000_000_000_000, 100_000_000_000_000).ToString(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await using var db = database.CreateContext();
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync();
        return supplier;
    }

    public static async Task<int?> GetBalanceAsync(TestDatabase database, Guid productId, Guid storageId, string batch = Batch)
    {
        await using var db = database.CreateContext();
        var balance = await db.StockBalances.AsNoTracking()
            .SingleOrDefaultAsync(b => b.ProductId == productId && b.StorageId == storageId && b.Batch == batch);
        return balance?.Balance;
    }

    public static async Task<List<StockMovement>> GetMovementsAsync(TestDatabase database, Guid productId, Guid storageId, string batch = Batch)
    {
        await using var db = database.CreateContext();
        return await db.StockMovements.AsNoTracking()
            .Where(m => m.ProductId == productId && m.StorageId == storageId && m.Batch == batch)
            .ToListAsync();
    }

    // A “prova real” do livro-razão: o saldo de cada lote do produto é igual à soma dos seus movimentos.
    public static async Task AssertLedgerClosesAsync(TestDatabase database, Guid productId)
    {
        await using var db = database.CreateContext();

        var balances = await db.StockBalances.AsNoTracking()
            .Where(b => b.ProductId == productId)
            .ToListAsync();

        var sums = await db.StockMovements.AsNoTracking()
            .Where(m => m.ProductId == productId)
            .GroupBy(m => new { m.StorageId, m.Batch })
            .Select(g => new { g.Key.StorageId, g.Key.Batch, Total = g.Sum(m => m.Quantity) })
            .ToListAsync();

        Assert.NotEmpty(balances);
        foreach (var balance in balances)
        {
            var total = sums.SingleOrDefault(s => s.StorageId == balance.StorageId && s.Batch == balance.Batch)?.Total ?? 0;
            Assert.Equal(balance.Balance, total);
        }
    }
}
