using Microsoft.EntityFrameworkCore;
using ProjectBee.Data;
using ProjectBee.Models;
using ProjectBee.Tests.Infrastructure;

namespace ProjectBee.Tests.Services;

public class UnitOfWorkTests : IClassFixture<TestDatabase>
{
    private readonly TestDatabase _database;

    public UnitOfWorkTests(TestDatabase database)
    {
        _database = database;
    }

    private sealed record Outcome(bool Success) : IOperationResult;

    private static Supplier NewSupplier() => new()
    {
        Name = "Fornecedor da unidade de trabalho",
        Document = Random.Shared.NextInt64(10_000_000_000_000, 100_000_000_000_000).ToString(),
        IsActive = true,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private async Task<bool> ExistsAsync(Guid supplierId)
    {
        await using var db = _database.CreateContext();
        return await db.Suppliers.AnyAsync(s => s.Id == supplierId);
    }

    [Fact]
    public async Task Successful_work_commits_every_save()
    {
        await using var db = _database.CreateContext();
        var first = NewSupplier();
        var second = NewSupplier();

        await new UnitOfWork(db).ExecuteAsync(async () =>
        {
            db.Suppliers.Add(first);
            await db.SaveChangesAsync();
            db.Suppliers.Add(second);
            await db.SaveChangesAsync();
            return new Outcome(true);
        });

        Assert.True(await ExistsAsync(first.Id));
        Assert.True(await ExistsAsync(second.Id));
    }

    // Uma regra recusada depois de um SaveChanges desfaz o que já tinha ido para o banco.
    [Fact]
    public async Task Refused_work_rolls_back_saves_already_made()
    {
        await using var db = _database.CreateContext();
        var supplier = NewSupplier();

        var outcome = await new UnitOfWork(db).ExecuteAsync(async () =>
        {
            db.Suppliers.Add(supplier);
            await db.SaveChangesAsync();
            return new Outcome(false);
        });

        Assert.False(outcome.Success);
        Assert.False(await ExistsAsync(supplier.Id));
        Assert.Empty(db.ChangeTracker.Entries());
    }

    [Fact]
    public async Task Exception_rolls_back_and_leaves_the_context_clean()
    {
        await using var db = _database.CreateContext();
        var supplier = NewSupplier();

        await Assert.ThrowsAsync<InvalidOperationException>(() => new UnitOfWork(db).ExecuteAsync<Outcome>(async () =>
        {
            db.Suppliers.Add(supplier);
            await db.SaveChangesAsync();
            throw new InvalidOperationException("Falha no meio do processo");
        }));

        Assert.False(await ExistsAsync(supplier.Id));
        Assert.Empty(db.ChangeTracker.Entries());
    }
}
