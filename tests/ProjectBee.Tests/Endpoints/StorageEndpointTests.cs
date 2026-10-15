using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ProjectBee.Tests.Infrastructure;
using static ProjectBee.Tests.Infrastructure.TestData;

namespace ProjectBee.Tests.Endpoints;

public class StorageEndpointTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _api;

    public StorageEndpointTests(ApiFactory api)
    {
        _api = api;
    }

    private static object UpdateBody(bool isActive) => new
    {
        name = "Armazém de teste",
        addressNumber = "100",
        addressStreet = "Rua dos Testes",
        addressCity = "São Paulo",
        isActive
    };

    private async Task<Guid> CreateStorageWithStockAsync(HttpClient client, int quantity)
    {
        var product = await CreateProductAsync(_api.Database);
        var storage = await CreateStorageAsync(_api.Database);
        (await client.PostAsJsonAsync("/api/movements/entry",
            new { productId = product.Id, storageId = storage.Id, batch = Batch, quantity })).EnsureSuccessStatusCode();
        return storage.Id;
    }

    private async Task<bool?> IsActiveAsync(Guid storageId)
    {
        await using var db = _api.Database.CreateContext();
        return (await db.Storages.AsNoTracking().SingleOrDefaultAsync(s => s.Id == storageId))?.IsActive;
    }

    [Fact]
    public async Task Deactivating_storage_with_stock_returns_422_and_keeps_it_active()
    {
        using var client = await _api.CreateManagerClientAsync();
        var storageId = await CreateStorageWithStockAsync(client, 12);

        var response = await client.PutAsJsonAsync($"/api/storages/{storageId}", UpdateBody(isActive: false));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("1 lote(s), somando 12 unidade(s)", body.GetProperty("detail").GetString());
        Assert.True(await IsActiveAsync(storageId));
    }

    [Fact]
    public async Task Deactivating_storage_without_stock_succeeds()
    {
        using var client = await _api.CreateManagerClientAsync();
        var storage = await CreateStorageAsync(_api.Database);

        var response = await client.PutAsJsonAsync($"/api/storages/{storage.Id}", UpdateBody(isActive: false));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.False(await IsActiveAsync(storage.Id));
    }

    [Fact]
    public async Task Deleting_storage_with_stock_returns_422()
    {
        using var client = await _api.CreateManagerClientAsync();
        var storageId = await CreateStorageWithStockAsync(client, 5);

        var response = await client.DeleteAsync($"/api/storages/{storageId}");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("O armazém ainda tem saldo. Transfira ou ajuste o estoque para zero antes de excluí-lo.",
            body.GetProperty("detail").GetString());
        Assert.NotNull(await IsActiveAsync(storageId));
    }

    [Fact]
    public async Task Deleting_storage_with_history_but_no_stock_returns_422()
    {
        using var client = await _api.CreateManagerClientAsync();
        var product = await CreateProductAsync(_api.Database);
        var storage = await CreateStorageAsync(_api.Database);
        (await client.PostAsJsonAsync("/api/movements/entry",
            new { productId = product.Id, storageId = storage.Id, batch = Batch, quantity = 3 })).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/movements/exit",
            new { productId = product.Id, storageId = storage.Id, batch = Batch, quantity = 3 })).EnsureSuccessStatusCode();

        var response = await client.DeleteAsync($"/api/storages/{storage.Id}");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.StartsWith("O armazém tem histórico de movimentos", body.GetProperty("detail").GetString());
        Assert.NotNull(await IsActiveAsync(storage.Id));
    }

    [Fact]
    public async Task Deleting_unused_storage_succeeds()
    {
        using var client = await _api.CreateManagerClientAsync();
        var storage = await CreateStorageAsync(_api.Database);

        var response = await client.DeleteAsync($"/api/storages/{storage.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Null(await IsActiveAsync(storage.Id));
    }
}
