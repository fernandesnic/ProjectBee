using ProjectBee.Models;
using ProjectBee.Services;
using ProjectBee.Tests.Infrastructure;
using static ProjectBee.Tests.Infrastructure.TestData;

namespace ProjectBee.Tests.Services;

public class StockMovementServiceTests : IClassFixture<TestDatabase>
{
    private const string User = "testes@projectbee.dev";
    private readonly TestDatabase _database;

    public StockMovementServiceTests(TestDatabase database)
    {
        _database = database;
    }

    // Cada chamada usa um DbContext novo, como acontece em uma requisição HTTP.
    private async Task<MovementResult> RunAsync(Func<StockMovementService, Task<MovementResult>> action)
    {
        await using var db = _database.CreateContext();
        return await action(new StockMovementService(db));
    }

    private async Task<(Product Product, Storage Storage)> CreateLotAsync(int initialBalance)
    {
        var product = await CreateProductAsync(_database);
        var storage = await CreateStorageAsync(_database);
        var entry = await RunAsync(s => s.RegisterEntryAsync(product.Id, storage.Id, Batch, initialBalance, User));
        Assert.True(entry.Success);
        return (product, storage);
    }

    // ---------- Entrada ----------

    [Fact]
    public async Task Entry_on_new_lot_creates_balance_and_movement()
    {
        var product = await CreateProductAsync(_database);
        var storage = await CreateStorageAsync(_database);

        var result = await RunAsync(s => s.RegisterEntryAsync(product.Id, storage.Id, Batch, 25, User));

        Assert.True(result.Success);
        Assert.Equal(25, result.Balance);
        Assert.Equal(25, await GetBalanceAsync(_database, product.Id, storage.Id));

        var movement = Assert.Single(await GetMovementsAsync(_database, product.Id, storage.Id));
        Assert.Equal(MovementType.Entry, movement.Type);
        Assert.Equal(25, movement.Quantity);
        Assert.Equal(User, movement.CreatedBy);
    }

    [Fact]
    public async Task Entry_on_existing_lot_adds_to_balance()
    {
        var (product, storage) = await CreateLotAsync(10);

        var result = await RunAsync(s => s.RegisterEntryAsync(product.Id, storage.Id, Batch, 5, User));

        Assert.True(result.Success);
        Assert.Equal(15, result.Balance);
        Assert.Equal(15, await GetBalanceAsync(_database, product.Id, storage.Id));
    }

    // ---------- Saída ----------

    [Fact]
    public async Task Exit_subtracts_and_records_negative_movement()
    {
        var (product, storage) = await CreateLotAsync(10);

        var result = await RunAsync(s => s.RegisterExitAsync(product.Id, storage.Id, Batch, 4, User));

        Assert.True(result.Success);
        Assert.Equal(6, result.Balance);
        Assert.Equal(6, await GetBalanceAsync(_database, product.Id, storage.Id));

        var exit = Assert.Single(await GetMovementsAsync(_database, product.Id, storage.Id), m => m.Type == MovementType.Exit);
        Assert.Equal(-4, exit.Quantity);
    }

    [Fact]
    public async Task Exit_with_insufficient_balance_is_refused_and_writes_nothing()
    {
        var (product, storage) = await CreateLotAsync(10);

        var result = await RunAsync(s => s.RegisterExitAsync(product.Id, storage.Id, Batch, 11, User));

        Assert.False(result.Success);
        Assert.False(result.IsConflict);
        Assert.Equal("Saldo insuficiente. Disponível: 10, solicitado: 11.", result.Error);
        Assert.Equal(10, await GetBalanceAsync(_database, product.Id, storage.Id));
        Assert.DoesNotContain(await GetMovementsAsync(_database, product.Id, storage.Id), m => m.Type == MovementType.Exit);
    }

    [Fact]
    public async Task Exit_from_missing_lot_is_refused()
    {
        var product = await CreateProductAsync(_database);
        var storage = await CreateStorageAsync(_database);

        var result = await RunAsync(s => s.RegisterExitAsync(product.Id, storage.Id, Batch, 1, User));

        Assert.False(result.Success);
        Assert.Equal("Não existe saldo para este produto, armazém e lote.", result.Error);
        Assert.Null(await GetBalanceAsync(_database, product.Id, storage.Id));
    }

    // ---------- Transferência ----------

    [Fact]
    public async Task Transfer_to_same_storage_is_refused()
    {
        var (product, storage) = await CreateLotAsync(10);

        var result = await RunAsync(s => s.RegisterTransferAsync(product.Id, storage.Id, storage.Id, Batch, 1, User));

        Assert.False(result.Success);
        Assert.Equal("O armazém de origem e o de destino devem ser diferentes.", result.Error);
        Assert.Equal(10, await GetBalanceAsync(_database, product.Id, storage.Id));
    }

    [Fact]
    public async Task Transfer_with_insufficient_balance_is_refused_and_writes_nothing()
    {
        var (product, source) = await CreateLotAsync(10);
        var destination = await CreateStorageAsync(_database);

        var result = await RunAsync(s => s.RegisterTransferAsync(product.Id, source.Id, destination.Id, Batch, 11, User));

        Assert.False(result.Success);
        Assert.Equal("Saldo insuficiente na origem. Disponível: 10, solicitado: 11.", result.Error);
        Assert.Equal(10, await GetBalanceAsync(_database, product.Id, source.Id));
        Assert.Null(await GetBalanceAsync(_database, product.Id, destination.Id));
    }

    [Fact]
    public async Task Transfer_moves_quantity_and_links_both_movements()
    {
        var (product, source) = await CreateLotAsync(10);
        var destination = await CreateStorageAsync(_database);

        var result = await RunAsync(s => s.RegisterTransferAsync(product.Id, source.Id, destination.Id, Batch, 4, User));

        Assert.True(result.Success);
        Assert.Equal(6, result.Balance); // saldo da origem
        Assert.Equal(6, await GetBalanceAsync(_database, product.Id, source.Id));
        Assert.Equal(4, await GetBalanceAsync(_database, product.Id, destination.Id));

        var transferOut = Assert.Single(await GetMovementsAsync(_database, product.Id, source.Id), m => m.Type == MovementType.TransferOut);
        var transferIn = Assert.Single(await GetMovementsAsync(_database, product.Id, destination.Id));
        Assert.Equal(MovementType.TransferIn, transferIn.Type);
        Assert.Equal(-4, transferOut.Quantity);
        Assert.Equal(4, transferIn.Quantity);
        Assert.NotNull(transferOut.TransferId);
        Assert.Equal(transferOut.TransferId, transferIn.TransferId);
    }

    // ---------- Ajuste ----------

    [Fact]
    public async Task Adjustment_without_difference_is_refused()
    {
        var (product, storage) = await CreateLotAsync(30);

        var result = await RunAsync(s => s.RegisterAdjustmentAsync(product.Id, storage.Id, Batch, 30, "Inventário mensal", User));

        Assert.False(result.Success);
        Assert.Equal("Não há diferença para ajustar. Saldo atual já é 30.", result.Error);
        Assert.DoesNotContain(await GetMovementsAsync(_database, product.Id, storage.Id), m => m.Type == MovementType.Adjustment);
    }

    [Theory]
    [InlineData(20, -10)] // faltou
    [InlineData(45, 15)]  // sobrou
    [InlineData(0, -30)]  // zerou
    public async Task Adjustment_sets_counted_quantity_and_records_signed_difference(int counted, int expectedDifference)
    {
        var (product, storage) = await CreateLotAsync(30);

        var result = await RunAsync(s => s.RegisterAdjustmentAsync(product.Id, storage.Id, Batch, counted, "Inventário mensal", User));

        Assert.True(result.Success);
        Assert.Equal(counted, result.Balance);
        Assert.Equal(counted, await GetBalanceAsync(_database, product.Id, storage.Id));

        var adjustment = Assert.Single(await GetMovementsAsync(_database, product.Id, storage.Id), m => m.Type == MovementType.Adjustment);
        Assert.Equal(expectedDifference, adjustment.Quantity);
        Assert.Equal("Inventário mensal", adjustment.Reason);
    }

    // ---------- Concorrência ----------

    // Reproduz o furo de forma determinística: a requisição B lê o saldo, a A grava uma saída,
    // e só então a B tenta gravar com o saldo que leu antes. Sem RowVersion, a B sobrescreveria a A.
    [Fact]
    public async Task Exit_based_on_stale_balance_returns_conflict_and_writes_nothing()
    {
        var (product, storage) = await CreateLotAsync(10);

        await using var requestB = _database.CreateContext();
        await requestB.StockBalances.FindAsync(product.Id, storage.Id, Batch); // B lê: saldo 10

        var exitA = await RunAsync(s => s.RegisterExitAsync(product.Id, storage.Id, Batch, 4, User));
        Assert.True(exitA.Success); // A grava: saldo 6

        var exitB = await new StockMovementService(requestB).RegisterExitAsync(product.Id, storage.Id, Batch, 3, User);

        Assert.False(exitB.Success);
        Assert.True(exitB.IsConflict);
        Assert.Equal(6, await GetBalanceAsync(_database, product.Id, storage.Id));
        Assert.Single(await GetMovementsAsync(_database, product.Id, storage.Id), m => m.Type == MovementType.Exit);
        await AssertLedgerClosesAsync(_database, product.Id);
    }

    // Sem o determinismo acima: várias saídas disparadas juntas. Quantas passam depende do timing,
    // mas a regra não: toda saída aceita tem movimento, e nenhuma atualização se perde.
    [Fact]
    public async Task Parallel_exits_never_lose_updates()
    {
        const int initial = 100;
        const int attempts = 20;
        var (product, storage) = await CreateLotAsync(initial);

        var results = await Task.WhenAll(Enumerable.Range(0, attempts).Select(_ =>
            Task.Run(() => RunAsync(s => s.RegisterExitAsync(product.Id, storage.Id, Batch, 1, User)))));

        Assert.All(results, r => Assert.True(r.Success || r.IsConflict));
        var accepted = results.Count(r => r.Success);
        Assert.True(accepted >= 1);

        Assert.Equal(initial - accepted, await GetBalanceAsync(_database, product.Id, storage.Id));
        Assert.Equal(accepted, (await GetMovementsAsync(_database, product.Id, storage.Id)).Count(m => m.Type == MovementType.Exit));
        await AssertLedgerClosesAsync(_database, product.Id);
    }

    // ---------- Livro-razão ----------

    [Fact]
    public async Task Ledger_closes_after_mixed_operations()
    {
        var (product, storageA) = await CreateLotAsync(50);
        var storageB = await CreateStorageAsync(_database);

        Assert.True((await RunAsync(s => s.RegisterExitAsync(product.Id, storageA.Id, Batch, 7, User))).Success);
        Assert.True((await RunAsync(s => s.RegisterTransferAsync(product.Id, storageA.Id, storageB.Id, Batch, 10, User))).Success);
        Assert.True((await RunAsync(s => s.RegisterAdjustmentAsync(product.Id, storageA.Id, Batch, 30, "Contagem cíclica", User))).Success);
        Assert.True((await RunAsync(s => s.RegisterEntryAsync(product.Id, storageB.Id, Batch, 5, User))).Success);
        Assert.False((await RunAsync(s => s.RegisterExitAsync(product.Id, storageB.Id, Batch, 999, User))).Success);

        Assert.Equal(30, await GetBalanceAsync(_database, product.Id, storageA.Id));
        Assert.Equal(15, await GetBalanceAsync(_database, product.Id, storageB.Id));
        await AssertLedgerClosesAsync(_database, product.Id);
    }
}
