using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ProjectBee.Tests.Infrastructure;
using static ProjectBee.Tests.Infrastructure.TestData;

namespace ProjectBee.Tests.Endpoints;

public class PurchaseOrderEndpointTests : IClassFixture<ApiFactory>
{
    private readonly ApiFactory _api;

    public PurchaseOrderEndpointTests(ApiFactory api)
    {
        _api = api;
    }

    private async Task<(JsonElement Order, Guid SupplierId, Guid ProductId, Guid StorageId)> CreateOrderAsync(HttpClient manager, int quantity = 10)
    {
        var supplier = await CreateSupplierAsync(_api.Database);
        var product = await CreateProductAsync(_api.Database);
        var storage = await CreateStorageAsync(_api.Database);

        var response = await manager.PostAsJsonAsync("/api/purchase-orders", new
        {
            supplierId = supplier.Id,
            storageId = storage.Id,
            notes = "Reposição mensal",
            items = new[] { new { productId = product.Id, quantity, unitPrice = 12.50m } }
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>(), supplier.Id, product.Id, storage.Id);
    }

    private static object ReceiptBody(JsonElement order, int quantity, string invoice = "NF-1") => new
    {
        invoiceNumber = invoice,
        rowVersion = order.GetProperty("rowVersion").GetString(),
        items = new[]
        {
            new
            {
                purchaseOrderItemId = order.GetProperty("items")[0].GetProperty("id").GetGuid(),
                batch = Batch,
                quantity,
                unitPrice = 12.50m
            }
        }
    };

    private static Guid IdOf(JsonElement order) => order.GetProperty("id").GetGuid();

    [Fact]
    public async Task Created_order_is_open_with_items_and_total()
    {
        using var manager = await _api.CreateManagerClientAsync();

        var (order, _, _, _) = await CreateOrderAsync(manager, 4);

        Assert.Equal("Open", order.GetProperty("status").GetString());
        Assert.True(order.GetProperty("number").GetInt32() > 0);
        var item = Assert.Single(order.GetProperty("items").EnumerateArray());
        Assert.Equal(4, item.GetProperty("quantity").GetInt32());
        Assert.Equal(0, item.GetProperty("receivedQuantity").GetInt32());

        var list = await manager.GetFromJsonAsync<JsonElement>("/api/purchase-orders?status=Open");
        var summary = Assert.Single(list.EnumerateArray(), o => o.GetProperty("id").GetGuid() == IdOf(order));
        Assert.Equal(50m, summary.GetProperty("total").GetDecimal());
    }

    [Fact]
    public async Task Order_without_items_or_with_repeated_product_returns_400()
    {
        using var manager = await _api.CreateManagerClientAsync();
        var supplier = await CreateSupplierAsync(_api.Database);
        var product = await CreateProductAsync(_api.Database);
        var storage = await CreateStorageAsync(_api.Database);

        var empty = await manager.PostAsJsonAsync("/api/purchase-orders",
            new { supplierId = supplier.Id, storageId = storage.Id, items = Array.Empty<object>() });
        var repeated = await manager.PostAsJsonAsync("/api/purchase-orders", new
        {
            supplierId = supplier.Id,
            storageId = storage.Id,
            items = new[]
            {
                new { productId = product.Id, quantity = 1, unitPrice = 1m },
                new { productId = product.Id, quantity = 2, unitPrice = 1m }
            }
        });

        Assert.Equal(HttpStatusCode.BadRequest, empty.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, repeated.StatusCode);
        var body = await repeated.Content.ReadFromJsonAsync<JsonElement>();
        Assert.StartsWith("Cada produto pode aparecer uma vez", body.GetProperty("errors").GetProperty("Items")[0].GetString());
    }

    [Fact]
    public async Task Operator_cannot_create_or_cancel_but_can_receive()
    {
        using var manager = await _api.CreateManagerClientAsync();
        using var operatorClient = await _api.CreateOperatorClientAsync();
        var (order, _, productId, storageId) = await CreateOrderAsync(manager);

        var create = await operatorClient.PostAsJsonAsync("/api/purchase-orders", new { });
        var cancel = await operatorClient.PostAsJsonAsync($"/api/purchase-orders/{IdOf(order)}/cancel",
            new { reason = "Sem permissão", rowVersion = order.GetProperty("rowVersion").GetString() });
        var receive = await operatorClient.PostAsJsonAsync($"/api/purchase-orders/{IdOf(order)}/receipts", ReceiptBody(order, 4));

        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, cancel.StatusCode);
        Assert.Equal(HttpStatusCode.OK, receive.StatusCode);

        var received = await receive.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Partial", received.GetProperty("status").GetString());
        Assert.Equal(4, received.GetProperty("items")[0].GetProperty("receivedQuantity").GetInt32());
        Assert.Equal("NF-1", Assert.Single(received.GetProperty("receipts").EnumerateArray()).GetProperty("invoiceNumber").GetString());
        Assert.Equal(4, await GetBalanceAsync(_api.Database, productId, storageId));

        // A entrada aparece no histórico do lote, com o tipo próprio do recebimento de compra.
        var history = await manager.GetFromJsonAsync<JsonElement>($"/api/movements?productId={productId}&storageId={storageId}");
        Assert.Equal("PurchaseReceipt", Assert.Single(history.EnumerateArray()).GetProperty("type").GetString());
    }

    [Fact]
    public async Task Receipt_above_pending_returns_422_with_detail()
    {
        using var manager = await _api.CreateManagerClientAsync();
        var (order, _, productId, storageId) = await CreateOrderAsync(manager, 10);

        var response = await manager.PostAsJsonAsync($"/api/purchase-orders/{IdOf(order)}/receipts", ReceiptBody(order, 12));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.StartsWith("Recebimento maior que o pedido", body.GetProperty("detail").GetString());
        Assert.Null(await GetBalanceAsync(_api.Database, productId, storageId));
    }

    [Fact]
    public async Task Receipt_without_RowVersion_returns_400()
    {
        using var manager = await _api.CreateManagerClientAsync();
        var (order, _, _, _) = await CreateOrderAsync(manager);

        var response = await manager.PostAsJsonAsync($"/api/purchase-orders/{IdOf(order)}/receipts", new
        {
            invoiceNumber = "NF-1",
            items = new[] { new { purchaseOrderItemId = order.GetProperty("items")[0].GetProperty("id").GetGuid(), batch = Batch, quantity = 1, unitPrice = 12.50m } }
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // O gerente abriu o pedido antes do recebimento; cancelar com a versão velha devolve 409.
    [Fact]
    public async Task Cancel_with_stale_RowVersion_returns_409_and_cancel_with_fresh_one_keeps_stock()
    {
        using var manager = await _api.CreateManagerClientAsync();
        var (order, _, productId, storageId) = await CreateOrderAsync(manager, 10);
        var receive = await manager.PostAsJsonAsync($"/api/purchase-orders/{IdOf(order)}/receipts", ReceiptBody(order, 3));
        var partial = await receive.Content.ReadFromJsonAsync<JsonElement>();

        var stale = await manager.PostAsJsonAsync($"/api/purchase-orders/{IdOf(order)}/cancel",
            new { reason = "Fornecedor sem estoque", rowVersion = order.GetProperty("rowVersion").GetString() });
        var fresh = await manager.PostAsJsonAsync($"/api/purchase-orders/{IdOf(order)}/cancel",
            new { reason = "Fornecedor sem estoque", rowVersion = partial.GetProperty("rowVersion").GetString() });

        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Equal(HttpStatusCode.OK, fresh.StatusCode);
        var cancelled = await fresh.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Cancelled", cancelled.GetProperty("status").GetString());
        Assert.Equal("Fornecedor sem estoque", cancelled.GetProperty("cancelReason").GetString());
        Assert.Equal(3, await GetBalanceAsync(_api.Database, productId, storageId));
    }

    [Fact]
    public async Task Unknown_order_returns_404()
    {
        using var manager = await _api.CreateManagerClientAsync();

        var response = await manager.GetAsync($"/api/purchase-orders/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Supplier_with_pending_order_cannot_be_deleted()
    {
        using var manager = await _api.CreateManagerClientAsync();
        var (_, supplierId, _, _) = await CreateOrderAsync(manager);

        var response = await manager.DeleteAsync($"/api/suppliers/{supplierId}");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }
}
