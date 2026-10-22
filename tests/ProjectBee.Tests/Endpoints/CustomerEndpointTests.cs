using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ProjectBee.Tests.Infrastructure;
using static ProjectBee.Tests.Infrastructure.TestData;

namespace ProjectBee.Tests.Endpoints;

public class CustomerEndpointTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _api;

    public CustomerEndpointTests(ApiFactory api)
    {
        _api = api;
    }

    private static object UpdateBody(byte[] rowVersion, string name) => new
    {
        name,
        email = "compras@cliente.dev",
        phone = "(11) 4000-1234",
        isActive = true,
        rowVersion
    };

    [Fact]
    public async Task Creating_customer_returns_201_and_reserves_the_document()
    {
        using var client = await _api.CreateManagerClientAsync();
        var document = (await CreateCustomerAsync(_api.Database)).Document;

        var repeated = await client.PostAsJsonAsync("/api/customers",
            new { name = "Outro cliente", document, email = "", phone = "" });

        Assert.Equal(HttpStatusCode.BadRequest, repeated.StatusCode);
        var body = await repeated.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Já existe um cliente com o documento informado, ativo ou excluído.",
            body.GetProperty("errors").GetProperty("Document")[0].GetString());

        var created = await client.PostAsJsonAsync("/api/customers",
            new { name = "Empório do Mel", document = "98765432000110", email = "", phone = "" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
    }

    [Fact]
    public async Task Document_with_mask_or_wrong_length_returns_400()
    {
        using var client = await _api.CreateManagerClientAsync();

        var response = await client.PostAsJsonAsync("/api/customers",
            new { name = "Cliente", document = "123.456.789-09", email = "", phone = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Editing_customer_with_stale_RowVersion_returns_409()
    {
        using var client = await _api.CreateManagerClientAsync();
        var customer = await CreateCustomerAsync(_api.Database);

        var first = await client.PutAsJsonAsync($"/api/customers/{customer.Id}", UpdateBody(customer.RowVersion, "Edição da Ana"));
        var second = await client.PutAsJsonAsync($"/api/customers/{customer.Id}", UpdateBody(customer.RowVersion, "Edição do Beto"));

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Operator_cannot_create_customer()
    {
        using var client = await _api.CreateOperatorClientAsync();

        var response = await client.PostAsJsonAsync("/api/customers",
            new { name = "Cliente", document = "12345678901", email = "", phone = "" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
