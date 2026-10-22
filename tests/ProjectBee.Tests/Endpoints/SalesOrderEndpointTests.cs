using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ProjectBee.Tests.Infrastructure;
using static ProjectBee.Tests.Infrastructure.TestData;

namespace ProjectBee.Tests.Endpoints;

public class SalesOrderEndpointTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _api;

    public SalesOrderEndpointTests(ApiFactory api)
    {
        _api = api;
    }

    // Produto de R$ 10,00 com saldo no armazém, pronto para vender.
    private async Task<(Guid CustomerId, Guid ProductId, Guid StorageId)> ArrangeStockAsync(HttpClient manager, int stock)
    {
        var customer = await CreateCustomerAsync(_api.Database);
        var product = await CreateProductAsync(_api.Database);
        var storage = await CreateStorageAsync(_api.Database);

        var entry = await manager.PostAsJsonAsync("/api/movements/entry",
            new { productId = product.Id, storageId = storage.Id, batch = Batch, quantity = stock });
        entry.EnsureSuccessStatusCode();

        return (customer.Id, product.Id, storage.Id);
    }

    private static Task<HttpResponseMessage> PostOrderAsync(HttpClient client, Guid customerId, Guid productId, Guid storageId,
        int quantity, string paymentMethod = "Boleto") =>
        client.PostAsJsonAsync("/api/sales-orders", new
        {
            customerId,
            storageId,
            paymentMethod,
            notes = "Venda de balcão",
            items = new[] { new { productId, quantity } }
        });

    private static object PayBody(JsonElement order) => new { rowVersion = order.GetProperty("rowVersion").GetString() };

    private static Guid IdOf(JsonElement order) => order.GetProperty("id").GetGuid();

    [Fact]
    public async Task Created_order_awaits_payment_with_boleto_at_product_price()
    {
        using var manager = await _api.CreateManagerClientAsync();
        var (customerId, productId, storageId) = await ArrangeStockAsync(manager, 50);

        var response = await PostOrderAsync(manager, customerId, productId, storageId, 3);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("AwaitingPayment", order.GetProperty("status").GetString());
        Assert.Equal("Boleto", order.GetProperty("paymentMethod").GetString());
        Assert.Equal(10m, order.GetProperty("items")[0].GetProperty("unitPrice").GetDecimal());

        var boleto = order.GetProperty("boleto");
        Assert.Equal("Pending", boleto.GetProperty("status").GetString());
        Assert.Equal(30m, boleto.GetProperty("amount").GetDecimal());
        Assert.Equal(47, boleto.GetProperty("digitableLine").GetString()!.Length);
        var todayInBrasilia = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "America/Sao_Paulo"));
        Assert.Equal(todayInBrasilia.AddDays(3), DateOnly.Parse(boleto.GetProperty("dueDate").GetString()!));
        Assert.Equal(JsonValueKind.Null, order.GetProperty("invoice").ValueKind);

        // Pedido não mexe no estoque: a baixa só acontece na nota.
        Assert.Equal(50, await GetBalanceAsync(_api.Database, productId, storageId));
    }

    [Fact]
    public async Task Order_beyond_available_stock_returns_422_counting_unbilled_orders()
    {
        using var manager = await _api.CreateManagerClientAsync();
        var (customerId, productId, storageId) = await ArrangeStockAsync(manager, 10);

        var tooMuch = await PostOrderAsync(manager, customerId, productId, storageId, 11);
        Assert.Equal((HttpStatusCode)422, tooMuch.StatusCode);

        // 7 já vendidos e não faturados: sobram 3 para o próximo pedido, mesmo com 10 no armazém.
        var first = await PostOrderAsync(manager, customerId, productId, storageId, 7);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await PostOrderAsync(manager, customerId, productId, storageId, 4);
        Assert.Equal((HttpStatusCode)422, second.StatusCode);
        var problem = await second.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("disponível 3, pedido 4", problem.GetProperty("detail").GetString());
    }

    [Theory]
    [InlineData("Pix")]
    [InlineData("Cheque")]
    public async Task Payment_method_other_than_boleto_returns_400(string paymentMethod)
    {
        using var manager = await _api.CreateManagerClientAsync();
        var (customerId, productId, storageId) = await ArrangeStockAsync(manager, 10);

        var response = await PostOrderAsync(manager, customerId, productId, storageId, 1, paymentMethod);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, body.GetProperty("errors").GetProperty("PaymentMethod").GetArrayLength());
    }

    [Fact]
    public async Task Paying_the_boleto_marks_the_order_paid_only_once()
    {
        using var manager = await _api.CreateManagerClientAsync();
        var (customerId, productId, storageId) = await ArrangeStockAsync(manager, 10);
        var order = await (await PostOrderAsync(manager, customerId, productId, storageId, 2)).Content.ReadFromJsonAsync<JsonElement>();

        var paid = await manager.PostAsJsonAsync($"/api/sales-orders/{IdOf(order)}/boleto/payment", PayBody(order));

        Assert.Equal(HttpStatusCode.OK, paid.StatusCode);
        var updated = await paid.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Paid", updated.GetProperty("status").GetString());
        Assert.Equal("Paid", updated.GetProperty("boleto").GetProperty("status").GetString());
        Assert.Equal(ApiFactory.DemoEmail, updated.GetProperty("boleto").GetProperty("paidBy").GetString());

        var again = await manager.PostAsJsonAsync($"/api/sales-orders/{IdOf(order)}/boleto/payment", PayBody(updated));
        Assert.Equal((HttpStatusCode)422, again.StatusCode);
    }

    [Fact]
    public async Task Paying_with_stale_RowVersion_returns_409()
    {
        using var manager = await _api.CreateManagerClientAsync();
        var (customerId, productId, storageId) = await ArrangeStockAsync(manager, 10);
        var order = await (await PostOrderAsync(manager, customerId, productId, storageId, 2)).Content.ReadFromJsonAsync<JsonElement>();

        await manager.PostAsJsonAsync($"/api/sales-orders/{IdOf(order)}/boleto/payment", PayBody(order));
        var stale = await manager.PostAsJsonAsync($"/api/sales-orders/{IdOf(order)}/boleto/payment", PayBody(order));

        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
    }

    [Fact]
    public async Task Operator_cannot_create_order_or_pay_boleto()
    {
        using var manager = await _api.CreateManagerClientAsync();
        using var operatorClient = await _api.CreateOperatorClientAsync();
        var (customerId, productId, storageId) = await ArrangeStockAsync(manager, 10);
        var order = await (await PostOrderAsync(manager, customerId, productId, storageId, 1)).Content.ReadFromJsonAsync<JsonElement>();

        var create = await PostOrderAsync(operatorClient, customerId, productId, storageId, 1);
        var pay = await operatorClient.PostAsJsonAsync($"/api/sales-orders/{IdOf(order)}/boleto/payment", PayBody(order));

        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, pay.StatusCode);
    }

    [Fact]
    public async Task Customer_with_unbilled_order_cannot_be_deleted()
    {
        using var manager = await _api.CreateManagerClientAsync();
        var (customerId, productId, storageId) = await ArrangeStockAsync(manager, 10);
        await PostOrderAsync(manager, customerId, productId, storageId, 1);

        var response = await manager.DeleteAsync($"/api/customers/{customerId}");

        Assert.Equal((HttpStatusCode)422, response.StatusCode);
    }
}
