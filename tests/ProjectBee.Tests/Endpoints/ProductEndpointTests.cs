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

    private async Task<DateTime?> DeletedAtAsync(Guid productId)
    {
        await using var db = _api.Database.CreateContext();
        return await db.Products.IgnoreQueryFilters().AsNoTracking()
            .Where(p => p.Id == productId).Select(p => p.DeletedAt).SingleAsync();
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

    // A exclusão é lógica, então o histórico não impede mais: a linha fica no banco para os movimentos.
    [Fact]
    public async Task Deleting_product_with_history_but_no_stock_soft_deletes_it()
    {
        using var client = await _api.CreateManagerClientAsync();
        var product = await CreateProductAsync(_api.Database);
        var storage = await CreateStorageAsync(_api.Database);
        (await client.PostAsJsonAsync("/api/movements/entry",
            new { productId = product.Id, storageId = storage.Id, batch = Batch, quantity = 3 })).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/movements/exit",
            new { productId = product.Id, storageId = storage.Id, batch = Batch, quantity = 3 })).EnsureSuccessStatusCode();

        var response = await client.DeleteAsync($"/api/products/{product.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.False(await ExistsAsync(product.Id));
        Assert.NotNull(await DeletedAtAsync(product.Id));

        // O lote zerado continua na tela de saldos, marcado como excluído, e o histórico vem completo.
        var stock = await client.GetFromJsonAsync<JsonElement>("/api/stock");
        var lot = stock.EnumerateArray().Single(s => s.GetProperty("productId").GetGuid() == product.Id);
        Assert.True(lot.GetProperty("productDeleted").GetBoolean());
        Assert.False(lot.GetProperty("storageDeleted").GetBoolean());

        var history = await client.GetFromJsonAsync<JsonElement>(
            $"/api/movements?productId={product.Id}&storageId={storage.Id}&batch={Batch}");
        Assert.Equal(2, history.GetArrayLength());
    }

    [Fact]
    public async Task Deleting_unused_product_succeeds()
    {
        using var client = await _api.CreateManagerClientAsync();
        var product = await CreateProductAsync(_api.Database);

        var response = await client.DeleteAsync($"/api/products/{product.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.False(await ExistsAsync(product.Id));
        Assert.NotNull(await DeletedAtAsync(product.Id));
    }

    private static object UpdateBody(string name, byte[]? rowVersion) =>
        new { name, desc = "Editado pelos testes", price = 15m, isActive = true, rowVersion };

    [Fact]
    public async Task Editing_product_with_stale_RowVersion_returns_409_and_keeps_first_edit()
    {
        using var client = await _api.CreateManagerClientAsync();
        var product = await CreateProductAsync(_api.Database);

        var first = await client.PutAsJsonAsync($"/api/products/{product.Id}", UpdateBody("Edição da Ana", product.RowVersion));
        var second = await client.PutAsJsonAsync($"/api/products/{product.Id}", UpdateBody("Edição do Beto", product.RowVersion));

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.StartsWith("O produto foi alterado por outra pessoa", await DetailAsync(second));

        await using var db = _api.Database.CreateContext();
        Assert.Equal("Edição da Ana", await db.Products.Where(p => p.Id == product.Id).Select(p => p.Name).SingleAsync());
    }

    [Fact]
    public async Task Editing_product_without_RowVersion_returns_400()
    {
        using var client = await _api.CreateManagerClientAsync();
        var product = await CreateProductAsync(_api.Database);

        var response = await client.PutAsJsonAsync($"/api/products/{product.Id}", UpdateBody("Sem versão", null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Antes, corpo que não desserializa (preço como texto) voltava 400 sem corpo nenhum.
    [Fact]
    public async Task Malformed_body_returns_400_with_problem_details()
    {
        using var client = await _api.CreateManagerClientAsync();

        var response = await client.PostAsync("/api/products", new StringContent(
            """{ "name": "Produto", "sku": "ABC-123", "desc": "x", "price": "doze reais" }""",
            System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Requisição inválida", body.GetProperty("title").GetString());
    }

    // Sem IgnoreQueryFilters na validação, o SKU do excluído passava e o índice único do banco devolvia 500.
    [Fact]
    public async Task Creating_product_with_SKU_of_deleted_product_returns_400()
    {
        using var client = await _api.CreateManagerClientAsync();
        var deleted = await CreateProductAsync(_api.Database);
        (await client.DeleteAsync($"/api/products/{deleted.Id}")).EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync("/api/products", new
        {
            name = "Produto novo",
            sku = deleted.SKU,
            desc = "Mesmo SKU do excluído",
            price = 12.5m
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await response.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("errors").GetProperty("SKU")
            .EnumerateArray().Select(e => e.GetString()).ToArray();
        Assert.Contains("Já existe um produto com o SKU informado, ativo ou excluído.", errors);
    }
}
