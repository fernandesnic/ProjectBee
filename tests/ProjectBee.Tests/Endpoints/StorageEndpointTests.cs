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

    private static object UpdateBody(bool isActive, byte[] rowVersion, string name = "Armazém de teste") => new
    {
        name,
        addressNumber = "100",
        addressStreet = "Rua dos Testes",
        addressCity = "São Paulo",
        isActive,
        rowVersion
    };

    private async Task<byte[]> RowVersionAsync(Guid storageId)
    {
        await using var db = _api.Database.CreateContext();
        return await db.Storages.AsNoTracking().Where(s => s.Id == storageId).Select(s => s.RowVersion).SingleAsync();
    }

    private async Task<string?> NameAsync(Guid storageId)
    {
        await using var db = _api.Database.CreateContext();
        return await db.Storages.AsNoTracking().Where(s => s.Id == storageId).Select(s => s.Name).SingleAsync();
    }

    // Duas pessoas abrem o mesmo armazém para editar: quem salva por último está com a versão velha.
    [Fact]
    public async Task Editing_storage_with_stale_RowVersion_returns_409_and_keeps_first_edit()
    {
        using var client = await _api.CreateManagerClientAsync();
        var storage = await CreateStorageAsync(_api.Database);

        var first = await client.PutAsJsonAsync($"/api/storages/{storage.Id}",
            UpdateBody(isActive: true, storage.RowVersion, name: "Edição da Ana"));
        var second = await client.PutAsJsonAsync($"/api/storages/{storage.Id}",
            UpdateBody(isActive: true, storage.RowVersion, name: "Edição do Beto"));

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        Assert.Equal("Edição da Ana", await NameAsync(storage.Id));

        // A resposta do PUT traz a versão nova: com ela, a próxima edição passa.
        var newVersion = (await first.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("rowVersion").GetBytesFromBase64();
        var third = await client.PutAsJsonAsync($"/api/storages/{storage.Id}",
            UpdateBody(isActive: true, newVersion, name: "Edição do Beto"));
        Assert.Equal(HttpStatusCode.OK, third.StatusCode);
    }

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

    private async Task<DateTime?> DeletedAtAsync(Guid storageId)
    {
        await using var db = _api.Database.CreateContext();
        return await db.Storages.IgnoreQueryFilters().AsNoTracking()
            .Where(s => s.Id == storageId).Select(s => s.DeletedAt).SingleAsync();
    }

    [Fact]
    public async Task Deactivating_storage_with_stock_returns_422_and_keeps_it_active()
    {
        using var client = await _api.CreateManagerClientAsync();
        var storageId = await CreateStorageWithStockAsync(client, 12);

        var response = await client.PutAsJsonAsync($"/api/storages/{storageId}", UpdateBody(isActive: false, await RowVersionAsync(storageId)));

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

        var response = await client.PutAsJsonAsync($"/api/storages/{storage.Id}", UpdateBody(isActive: false, storage.RowVersion));

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
    public async Task Deleting_storage_with_history_but_no_stock_soft_deletes_it()
    {
        using var client = await _api.CreateManagerClientAsync();
        var product = await CreateProductAsync(_api.Database);
        var storage = await CreateStorageAsync(_api.Database);
        (await client.PostAsJsonAsync("/api/movements/entry",
            new { productId = product.Id, storageId = storage.Id, batch = Batch, quantity = 3 })).EnsureSuccessStatusCode();
        (await client.PostAsJsonAsync("/api/movements/exit",
            new { productId = product.Id, storageId = storage.Id, batch = Batch, quantity = 3 })).EnsureSuccessStatusCode();

        var response = await client.DeleteAsync($"/api/storages/{storage.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Null(await IsActiveAsync(storage.Id));
        Assert.NotNull(await DeletedAtAsync(storage.Id));
    }

    [Fact]
    public async Task Deleting_unused_storage_succeeds()
    {
        using var client = await _api.CreateManagerClientAsync();
        var storage = await CreateStorageAsync(_api.Database);

        var response = await client.DeleteAsync($"/api/storages/{storage.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Null(await IsActiveAsync(storage.Id));
        Assert.NotNull(await DeletedAtAsync(storage.Id));
    }

    private static object CreateBody(string idNumber) => new
    {
        idNumber,
        name = "Armazém de teste",
        addressNumber = "100",
        addressStreet = "Rua dos Testes",
        addressCity = "São Paulo"
    };

    private static async Task<string[]> FieldErrorsAsync(HttpResponseMessage response, string field) =>
        FieldErrors(await response.Content.ReadFromJsonAsync<JsonElement>(), field);

    private static string[] FieldErrors(JsonElement body, string field) =>
        body.GetProperty("errors").GetProperty(field)
            .EnumerateArray().Select(e => e.GetString()!).ToArray();

    [Fact]
    public async Task Creating_storage_with_repeated_IdNumber_returns_400()
    {
        using var client = await _api.CreateManagerClientAsync();
        var existing = await CreateStorageAsync(_api.Database);

        var response = await client.PostAsJsonAsync("/api/storages", CreateBody(existing.IdNumber));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Já existe um armazém com o código informado.", await FieldErrorsAsync(response, "IdNumber"));
    }

    // O código de um armazém excluído continua reservado: o histórico dele ainda aparece com esse código.
    [Fact]
    public async Task Creating_storage_with_IdNumber_of_deleted_storage_returns_400()
    {
        using var client = await _api.CreateManagerClientAsync();
        var deleted = await CreateStorageAsync(_api.Database);
        (await client.DeleteAsync($"/api/storages/{deleted.Id}")).EnsureSuccessStatusCode();

        var response = await client.PostAsJsonAsync("/api/storages", CreateBody(deleted.IdNumber));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Já existe um armazém com o código informado.", await FieldErrorsAsync(response, "IdNumber"));
    }

    [Fact]
    public async Task Creating_storage_with_empty_fields_returns_messages_in_portuguese()
    {
        using var client = await _api.CreateManagerClientAsync();

        var response = await client.PostAsJsonAsync("/api/storages", new
        {
            idNumber = "",
            name = "",
            addressNumber = "",
            addressStreet = "",
            addressCity = ""
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("A rua é obrigatória.", FieldErrors(body, "AddressStreet"));
        Assert.Contains("A cidade é obrigatória.", FieldErrors(body, "AddressCity"));
        Assert.Contains("O número do endereço é obrigatório.", FieldErrors(body, "AddressNumber"));
        Assert.Contains("O nome é obrigatório.", FieldErrors(body, "Name"));
        Assert.Contains("O código do armazém é obrigatório.", FieldErrors(body, "IdNumber"));
    }
}
