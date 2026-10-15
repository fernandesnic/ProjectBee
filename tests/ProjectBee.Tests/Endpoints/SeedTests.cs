using Microsoft.EntityFrameworkCore;
using ProjectBee.Models;
using ProjectBee.Tests.Infrastructure;

namespace ProjectBee.Tests.Endpoints;

// Fixture própria: o banco só tem o que o startup semeou, como num clone novo do projeto.
public class SeedTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _api;

    public SeedTests(ApiFactory api)
    {
        _api = api;
    }

    [Fact]
    public async Task Demo_data_passes_the_ledger_proof_on_first_boot()
    {
        using var client = _api.CreateClient(); // sobe a API: migrations já aplicadas, seed roda aqui

        await using var db = _api.Database.CreateContext();
        var balances = await db.StockBalances.AsNoTracking().ToListAsync();
        var sums = await db.StockMovements.AsNoTracking()
            .GroupBy(m => new { m.ProductId, m.StorageId, m.Batch })
            .Select(g => new { g.Key.ProductId, g.Key.StorageId, g.Key.Batch, Total = g.Sum(m => m.Quantity) })
            .ToListAsync();

        Assert.NotEmpty(balances);
        Assert.All(balances, balance =>
        {
            var total = sums.SingleOrDefault(s =>
                s.ProductId == balance.ProductId && s.StorageId == balance.StorageId && s.Batch == balance.Batch)?.Total ?? 0;
            Assert.Equal(balance.Balance, total);
        });

        Assert.All(await db.StockMovements.AsNoTracking().ToListAsync(),
            m => Assert.Equal(MovementType.OpeningBalance, m.Type));
    }
}
