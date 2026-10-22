using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ProjectBee.Models;
using ProjectBee.Tests.Infrastructure;
using static ProjectBee.Tests.Infrastructure.TestData;

namespace ProjectBee.Tests.Endpoints;

public class InvoiceEndpointTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _api;

    public InvoiceEndpointTests(ApiFactory api)
    {
        _api = api;
    }

    private record Arranged(Guid CustomerId, Guid ProductId, Guid StorageId);

    // Cada lote entra na ordem dada: é essa ordem (e não o nome do lote) que o FIFO segue.
    private async Task<Arranged> ArrangeStockAsync(HttpClient manager, params (string Batch, int Quantity)[] entries)
    {
        var customer = await CreateCustomerAsync(_api.Database);
        var product = await CreateProductAsync(_api.Database);
        var storage = await CreateStorageAsync(_api.Database);

        foreach (var (batch, quantity) in entries)
        {
            var entry = await manager.PostAsJsonAsync("/api/movements/entry",
                new { productId = product.Id, storageId = storage.Id, batch, quantity });
            entry.EnsureSuccessStatusCode();
        }

        return new Arranged(customer.Id, product.Id, storage.Id);
    }

    private static async Task<JsonElement> CreateOrderAsync(HttpClient manager, Arranged a, int quantity)
    {
        var response = await manager.PostAsJsonAsync("/api/sales-orders", new
        {
            customerId = a.CustomerId,
            storageId = a.StorageId,
            paymentMethod = "Boleto",
            items = new[] { new { productId = a.ProductId, quantity } }
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<JsonElement> PayAsync(HttpClient manager, JsonElement order)
    {
        var response = await manager.PostAsJsonAsync($"/api/sales-orders/{IdOf(order)}/boleto/payment", RowVersionOf(order));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private async Task<JsonElement> PaidOrderAsync(HttpClient manager, Arranged a, int quantity) =>
        await PayAsync(manager, await CreateOrderAsync(manager, a, quantity));

    private static Task<HttpResponseMessage> InvoiceAsync(HttpClient client, JsonElement order) =>
        client.PostAsJsonAsync("/api/invoices", new
        {
            salesOrderId = IdOf(order),
            rowVersion = order.GetProperty("rowVersion").GetString()
        });

    private static object RowVersionOf(JsonElement order) => new { rowVersion = order.GetProperty("rowVersion").GetString() };

    private static Guid IdOf(JsonElement order) => order.GetProperty("id").GetGuid();

    [Fact]
    public async Task Invoicing_a_paid_order_ships_the_stock_and_closes_the_ledger()
    {
        using var manager = await _api.CreateManagerClientAsync();
        var a = await ArrangeStockAsync(manager, (Batch, 20));
        var order = await PaidOrderAsync(manager, a, 6);

        var response = await InvoiceAsync(manager, order);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var invoice = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(60m, invoice.GetProperty("total").GetDecimal());
        Assert.Equal(order.GetProperty("number").GetInt32(), invoice.GetProperty("salesOrderNumber").GetInt32());

        Assert.Equal(14, await GetBalanceAsync(_api.Database, a.ProductId, a.StorageId));
        var exit = Assert.Single(await GetMovementsAsync(_api.Database, a.ProductId, a.StorageId),
            m => m.Type == MovementType.SalesInvoice);
        Assert.Equal(-6, exit.Quantity);
        Assert.Equal(invoice.GetProperty("id").GetGuid(), exit.SalesInvoiceId);
        await AssertLedgerClosesAsync(_api.Database, a.ProductId);

        var detail = await manager.GetFromJsonAsync<JsonElement>($"/api/sales-orders/{IdOf(order)}");
        Assert.Equal("Invoiced", detail.GetProperty("status").GetString());
        Assert.Equal(invoice.GetProperty("number").GetInt32(), detail.GetProperty("invoice").GetProperty("number").GetInt32());
    }

    [Fact]
    public async Task Batches_ship_first_in_first_out_across_lots()
    {
        using var manager = await _api.CreateManagerClientAsync();
        // LOTE-B entrou antes de LOTE-A: sai primeiro, mesmo vindo depois em ordem alfabética.
        var a = await ArrangeStockAsync(manager, ("LOTE-B", 5), ("LOTE-A", 10));
        var order = await PaidOrderAsync(manager, a, 8);

        var invoice = await (await InvoiceAsync(manager, order)).Content.ReadFromJsonAsync<JsonElement>();

        var shipped = invoice.GetProperty("items").EnumerateArray()
            .ToDictionary(i => i.GetProperty("batch").GetString()!, i => i.GetProperty("quantity").GetInt32());
        Assert.Equal(new Dictionary<string, int> { ["LOTE-A"] = 3, ["LOTE-B"] = 5 }, shipped);
        Assert.Equal(0, await GetBalanceAsync(_api.Database, a.ProductId, a.StorageId, "LOTE-B"));
        Assert.Equal(7, await GetBalanceAsync(_api.Database, a.ProductId, a.StorageId, "LOTE-A"));
        await AssertLedgerClosesAsync(_api.Database, a.ProductId);
    }

    [Fact]
    public async Task Unpaid_or_already_invoiced_order_returns_422_and_ships_nothing_more()
    {
        using var manager = await _api.CreateManagerClientAsync();
        var a = await ArrangeStockAsync(manager, (Batch, 20));
        var order = await CreateOrderAsync(manager, a, 5);

        var unpaid = await InvoiceAsync(manager, order);
        Assert.Equal((HttpStatusCode)422, unpaid.StatusCode);
        Assert.Equal(20, await GetBalanceAsync(_api.Database, a.ProductId, a.StorageId));

        var paid = await PayAsync(manager, order);
        Assert.Equal(HttpStatusCode.Created, (await InvoiceAsync(manager, paid)).StatusCode);

        var current = await manager.GetFromJsonAsync<JsonElement>($"/api/sales-orders/{IdOf(order)}");
        var twice = await InvoiceAsync(manager, current);
        Assert.Equal((HttpStatusCode)422, twice.StatusCode);
        Assert.Equal(15, await GetBalanceAsync(_api.Database, a.ProductId, a.StorageId));
    }

    [Fact]
    public async Task Stock_gone_before_invoicing_returns_422_and_rolls_back()
    {
        using var manager = await _api.CreateManagerClientAsync();
        var a = await ArrangeStockAsync(manager, (Batch, 10));
        var order = await PaidOrderAsync(manager, a, 8);

        // Uma saída avulsa leva parte do saldo depois da venda.
        var exit = await manager.PostAsJsonAsync("/api/movements/exit",
            new { productId = a.ProductId, storageId = a.StorageId, batch = Batch, quantity = 5 });
        exit.EnsureSuccessStatusCode();

        var response = await InvoiceAsync(manager, order);

        Assert.Equal((HttpStatusCode)422, response.StatusCode);
        Assert.Equal(5, await GetBalanceAsync(_api.Database, a.ProductId, a.StorageId));
        var detail = await manager.GetFromJsonAsync<JsonElement>($"/api/sales-orders/{IdOf(order)}");
        Assert.Equal("Paid", detail.GetProperty("status").GetString());
    }

    [Fact]
    public async Task Simultaneous_invoicing_ships_the_order_only_once()
    {
        using var manager = await _api.CreateManagerClientAsync();
        using var operatorClient = await _api.CreateOperatorClientAsync();
        var a = await ArrangeStockAsync(manager, (Batch, 50));
        var order = await PaidOrderAsync(manager, a, 10);

        var responses = await Task.WhenAll(
            Enumerable.Range(0, 5).Select(i => InvoiceAsync(i % 2 == 0 ? manager : operatorClient, order)));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        Assert.All(responses.Where(r => r.StatusCode != HttpStatusCode.Created),
            r => Assert.Contains(r.StatusCode, new[] { HttpStatusCode.Conflict, (HttpStatusCode)422 }));
        Assert.Equal(40, await GetBalanceAsync(_api.Database, a.ProductId, a.StorageId));
        await AssertLedgerClosesAsync(_api.Database, a.ProductId);
    }

    [Fact]
    public async Task Invoices_are_listed_newest_first()
    {
        using var manager = await _api.CreateManagerClientAsync();
        var a = await ArrangeStockAsync(manager, (Batch, 20));
        var first = await (await InvoiceAsync(manager, await PaidOrderAsync(manager, a, 1))).Content.ReadFromJsonAsync<JsonElement>();
        var second = await (await InvoiceAsync(manager, await PaidOrderAsync(manager, a, 2))).Content.ReadFromJsonAsync<JsonElement>();

        var list = await manager.GetFromJsonAsync<JsonElement>($"/api/invoices?customerId={a.CustomerId}");

        Assert.Equal(
            new[] { second.GetProperty("number").GetInt32(), first.GetProperty("number").GetInt32() },
            list.EnumerateArray().Select(i => i.GetProperty("number").GetInt32()));
    }
}
