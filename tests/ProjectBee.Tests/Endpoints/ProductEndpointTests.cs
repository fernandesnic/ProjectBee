using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ProjectBee.Tests.Infrastructure;
using static ProjectBee.Tests.Infrastructure.TestData;

namespace ProjectBee.Tests.Endpoints;

public class ProductEndpointTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _api;

    public ProductEndpointTests(ApiFactory api)
    {
        _api = api;
    }

    private async Task<bool> ExistsAsync(Guid productId)
    {
        await using var db = _api.Database.CreateContext();
        return await db.Products.AsNoTracking().AnyAsync(p => p.Id == productId);
    }

    private static async Task<string?> DetailAsync(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("detail").GetString();

    [Fact]
    public async Task Deleting_product_with_stock_returns_422()
    {
        using var client = await _api.CreateManagerClientAsync();
        var product = await CreateProductAsync(_api.Database);
        var storage = await CreateStorageAsync(_api.Database);
        (await client.PostAsJsonAsync("/api/movements/entry",
            new { productId = product.Id, storageId = storage.Id, batch = Batch, quantity = 5 })).EnsureSuccessStatusCode();

        var response = await client.DeleteAsync($"/api/products/{product.Id}");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.StartsWith("O produto ainda tem saldo em estoque", await DetailAsync(response));
        Assert.True(await ExistsAsync(product.Id));
    }

    // Antes desta regra, o Restrict dos movimentos fazia o banco recusar e a API devolvia 500.
    [Fact]
    public async Task Deleting_product_with_history_but_no_stock_returns_422()
    {
        using var client = await _api.CreateManagerClientAsync();
        var product = await CreateProductAsync(_api.Database);
        var storage = await CreateStorageAsync(_api.Database);
        (await client.PostAsJsonAsync("/api/movements/entry",
            new { productId = product.Id, storageId = storage.Id, batch = Batch, quantity = 3 })).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/movements/exit",
            new { productId = product.Id, storageId = storage.Id, batch = Batch, quantity = 3 })).EnsureSuccessStatusCode();

        var response = await client.DeleteAsync($"/api/products/{product.Id}");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.StartsWith("O produto tem histórico de movimentos", await DetailAsync(response));
        Assert.True(await ExistsAsync(product.Id));
    }

    [Fact]
    public async Task Deleting_unused_product_succeeds()
    {
        using var client = await _api.CreateManagerClientAsync();
        var product = await CreateProductAsync(_api.Database);

        var response = await client.DeleteAsync($"/api/products/{product.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.False(await ExistsAsync(product.Id));
    }
}
