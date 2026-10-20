using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ProjectBee.Tests.Infrastructure;
using static ProjectBee.Tests.Infrastructure.TestData;

namespace ProjectBee.Tests.Endpoints;

public class SupplierEndpointTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _api;

    public SupplierEndpointTests(ApiFactory api)
    {
        _api = api;
    }

    private static object UpdateBody(byte[] rowVersion, string name) => new
    {
        name,
        email = "compras@fornecedor.dev",
        phone = "(11) 4000-1234",
        isActive = true,
        rowVersion
    };

    [Fact]
    public async Task Creating_supplier_returns_201_and_reserves_the_document()
    {
        using var client = await _api.CreateManagerClientAsync();
        var document = (await CreateSupplierAsync(_api.Database)).Document;

        var repeated = await client.PostAsJsonAsync("/api/suppliers",
            new { name = "Outro fornecedor", document, email = "", phone = "" });

        Assert.Equal(HttpStatusCode.BadRequest, repeated.StatusCode);
        var body = await repeated.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Já existe um fornecedor com o documento informado, ativo ou excluído.",
            body.GetProperty("errors").GetProperty("Document")[0].GetString());

        var created = await client.PostAsJsonAsync("/api/suppliers",
            new { name = "Apiário Novo", document = "12345678000199", email = "", phone = "" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
    }

    [Fact]
    public async Task Document_with_mask_or_wrong_length_returns_400()
    {
        using var client = await _api.CreateManagerClientAsync();

        var response = await client.PostAsJsonAsync("/api/suppliers",
            new { name = "Fornecedor", document = "12.345.678/0001-99", email = "", phone = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Editing_supplier_with_stale_RowVersion_returns_409()
    {
        using var client = await _api.CreateManagerClientAsync();
        var supplier = await CreateSupplierAsync(_api.Database);

        var first = await client.PutAsJsonAsync($"/api/suppliers/{supplier.Id}", UpdateBody(supplier.RowVersion, "Edição da Ana"));
        var second = await client.PutAsJsonAsync($"/api/suppliers/{supplier.Id}", UpdateBody(supplier.RowVersion, "Edição do Beto"));

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Operator_cannot_create_supplier()
    {
        using var client = await _api.CreateOperatorClientAsync();

        var response = await client.PostAsJsonAsync("/api/suppliers",
            new { name = "Fornecedor", document = "12345678901", email = "", phone = "" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
