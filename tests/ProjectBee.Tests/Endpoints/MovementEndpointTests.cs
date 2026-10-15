using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ProjectBee.Models;
using ProjectBee.Tests.Infrastructure;
using static ProjectBee.Tests.Infrastructure.TestData;

namespace ProjectBee.Tests.Endpoints;

public class MovementEndpointTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _api;

    public MovementEndpointTests(ApiFactory api)
    {
        _api = api;
    }

    private async Task<(Guid ProductId, Guid StorageId)> CreateLotAsync(HttpClient client, int quantity)
    {
        var product = await CreateProductAsync(_api.Database);
        var storage = await CreateStorageAsync(_api.Database);

        var entry = await client.PostAsJsonAsync("/api/movements/entry",
            new { productId = product.Id, storageId = storage.Id, batch = Batch, quantity });
        entry.EnsureSuccessStatusCode();

        return (product.Id, storage.Id);
    }

    [Fact]
    public async Task Entry_returns_message_and_current_balance()
    {
        using var client = await _api.CreateManagerClientAsync();
        var (productId, storageId) = await CreateLotAsync(client, 10);

        var response = await client.PostAsJsonAsync("/api/movements/entry",
            new { productId, storageId, batch = Batch, quantity = 5 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Entrada registrada com sucesso!", body.GetProperty("mensagem").GetString());
        Assert.Equal(15, body.GetProperty("saldoAtual").GetInt32());
    }

    [Fact]
    public async Task Invalid_quantity_returns_400_with_field_errors()
    {
        using var client = await _api.CreateManagerClientAsync();
        var (productId, storageId) = await CreateLotAsync(client, 10);

        var response = await client.PostAsJsonAsync("/api/movements/exit",
            new { productId, storageId, batch = Batch, quantity = 0 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var errors = body.GetProperty("errors").GetProperty("Quantity");
        Assert.Equal("A quantidade deve ser maior que zero.", errors[0].GetString());
    }

    [Fact]
    public async Task Exit_with_insufficient_balance_returns_422_with_detail()
    {
        using var client = await _api.CreateManagerClientAsync();
        var (productId, storageId) = await CreateLotAsync(client, 10);

        var response = await client.PostAsJsonAsync("/api/movements/exit",
            new { productId, storageId, batch = Batch, quantity = 11 });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Saldo insuficiente. Disponível: 10, solicitado: 11.", body.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Request_without_token_returns_401()
    {
        using var client = _api.CreateClient();

        var response = await client.PostAsJsonAsync("/api/movements/entry",
            new { productId = Guid.NewGuid(), storageId = Guid.NewGuid(), batch = Batch, quantity = 1 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Operator_cannot_register_adjustment()
    {
        using var manager = await _api.CreateManagerClientAsync();
        var (productId, storageId) = await CreateLotAsync(manager, 10);
        using var operatorClient = await _api.CreateOperatorClientAsync();

        var response = await operatorClient.PostAsJsonAsync("/api/movements/adjustment",
            new { productId, storageId, batch = Batch, countedQuantity = 8, reason = "Contagem do operador" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(10, await GetBalanceAsync(_api.Database, productId, storageId));
    }

    [Fact]
    public async Task Operator_can_register_exit()
    {
        using var manager = await _api.CreateManagerClientAsync();
        var (productId, storageId) = await CreateLotAsync(manager, 10);
        using var operatorClient = await _api.CreateOperatorClientAsync();

        var response = await operatorClient.PostAsJsonAsync("/api/movements/exit",
            new { productId, storageId, batch = Batch, quantity = 3 });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(7, await GetBalanceAsync(_api.Database, productId, storageId));
    }

    // Mesmo cenário que derrubaria o saldo sem o RowVersion, agora pela API inteira.
    [Fact]
    public async Task Concurrent_exits_answer_200_or_409_and_never_lose_updates()
    {
        const int initial = 100;
        const int attempts = 20;
        using var client = await _api.CreateManagerClientAsync();
        var (productId, storageId) = await CreateLotAsync(client, initial);

        var responses = await Task.WhenAll(Enumerable.Range(0, attempts).Select(_ =>
            client.PostAsJsonAsync("/api/movements/exit", new { productId, storageId, batch = Batch, quantity = 1 })));

        Assert.All(responses, r => Assert.Contains(r.StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.Conflict }));
        var accepted = responses.Count(r => r.StatusCode == HttpStatusCode.OK);
        Assert.True(accepted >= 1);

        Assert.Equal(initial - accepted, await GetBalanceAsync(_api.Database, productId, storageId));
        Assert.Equal(accepted, (await GetMovementsAsync(_api.Database, productId, storageId)).Count(m => m.Type == MovementType.Exit));
        await AssertLedgerClosesAsync(_api.Database, productId);
    }

    [Fact]
    public async Task History_lists_lot_movements_newest_first()
    {
        using var client = await _api.CreateManagerClientAsync();
        var (productId, storageId) = await CreateLotAsync(client, 10);
        (await client.PostAsJsonAsync("/api/movements/exit", new { productId, storageId, batch = Batch, quantity = 2 }))
            .EnsureSuccessStatusCode();

        var history = await client.GetFromJsonAsync<JsonElement>(
            $"/api/movements?productId={productId}&storageId={storageId}&batch={Batch}");

        Assert.Equal(2, history.GetArrayLength());
        Assert.Equal("Exit", history[0].GetProperty("type").GetString());
        Assert.Equal(-2, history[0].GetProperty("quantity").GetInt32());
        Assert.Equal("Entry", history[1].GetProperty("type").GetString());
    }

    // Garante que o caminho sem histórico (passo 16) não volte: /api/stock só aceita GET.
    [Fact]
    public async Task Stock_endpoint_is_read_only()
    {
        using var client = await _api.CreateManagerClientAsync();

        var get = await client.GetAsync("/api/stock");
        var post = await client.PostAsJsonAsync("/api/stock",
            new { productId = Guid.NewGuid(), storageId = Guid.NewGuid(), balance = 999, batch = Batch });

        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, post.StatusCode);
    }
}
