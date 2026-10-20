using Microsoft.EntityFrameworkCore;
using ProjectBee.Data;
using ProjectBee.DTO.Purchase;
using ProjectBee.Models;
using ProjectBee.Services;
using ProjectBee.Tests.Infrastructure;
using static ProjectBee.Tests.Infrastructure.TestData;

namespace ProjectBee.Tests.Services;

public class PurchaseServiceTests : IClassFixture<TestDatabase>
{
    private const string User = "compras@projectbee.dev";
    private readonly TestDatabase _database;

    public PurchaseServiceTests(TestDatabase database)
    {
        _database = database;
    }

    private sealed class FailingListener : IPurchaseReceiptListener
    {
        public Task OnReceiptRegisteredAsync(PurchaseOrder order, PurchaseReceipt receipt) =>
            throw new InvalidOperationException("Contas a pagar indisponível");
    }

    // Cada chamada usa um DbContext novo, como acontece em uma requisição HTTP.
    private async Task<PurchaseResult> RunAsync(
        Func<PurchaseService, Task<PurchaseResult>> action, params IPurchaseReceiptListener[] listeners)
    {
        await using var db = _database.CreateContext();
        var service = new PurchaseService(db, new UnitOfWork(db), new StockMovementService(db), listeners);
        return await action(service);
    }

    private async Task<PurchaseOrder> LoadAsync(Guid orderId)
    {
        await using var db = _database.CreateContext();
        return await db.PurchaseOrders.AsNoTracking()
            .Include(o => o.Items)
            .Include(o => o.Receipts).ThenInclude(r => r.Items)
            .SingleAsync(o => o.Id == orderId);
    }

    // Pedido de 10 unidades a R$ 12,50 de um produto novo, num armazém novo.
    private async Task<(PurchaseOrder Order, Product Product, Storage Storage)> CreateOrderAsync(int quantity = 10)
    {
        var supplier = await CreateSupplierAsync(_database);
        var product = await CreateProductAsync(_database);
        var storage = await CreateStorageAsync(_database);

        var result = await RunAsync(s => s.CreateOrderAsync(
            new CreatePurchaseOrderDTO(supplier.Id, storage.Id, null, [new PurchaseOrderItemDTO(product.Id, quantity, 12.50m)]),
            User));

        Assert.True(result.Success);
        return (await LoadAsync(result.OrderId!.Value), product, storage);
    }

    private static ReceivePurchaseOrderDTO Receipt(PurchaseOrder order, int quantity, decimal unitPrice = 12.50m,
        string invoice = "NF-1", string? divergenceReason = null) =>
        new(invoice, divergenceReason, [new ReceiptItemDTO(order.Items[0].Id, Batch, quantity, unitPrice)], order.RowVersion);

    [Fact]
    public async Task New_order_is_open_with_sequential_number()
    {
        var (first, _, _) = await CreateOrderAsync();
        var (second, _, _) = await CreateOrderAsync();

        Assert.Equal(PurchaseOrderStatus.Open, first.Status);
        Assert.True(second.Number > first.Number);
        Assert.Equal(User, first.CreatedBy);
    }

    // ---------- Recebimento menor e igual ao pedido ----------

    [Fact]
    public async Task Partial_receipt_posts_stock_and_leaves_order_partial()
    {
        var (order, product, storage) = await CreateOrderAsync(10);

        var result = await RunAsync(s => s.ReceiveAsync(order.Id, Receipt(order, 4), User));

        Assert.True(result.Success);
        var saved = await LoadAsync(order.Id);
        Assert.Equal(PurchaseOrderStatus.Partial, saved.Status);
        Assert.Equal(4, saved.Items[0].ReceivedQuantity);
        Assert.Equal(4, await GetBalanceAsync(_database, product.Id, storage.Id));

        var movement = Assert.Single(await GetMovementsAsync(_database, product.Id, storage.Id));
        Assert.Equal(MovementType.PurchaseReceipt, movement.Type);
        Assert.Equal(Assert.Single(saved.Receipts).Id, movement.PurchaseReceiptId);
        await AssertLedgerClosesAsync(_database, product.Id);
    }

    [Fact]
    public async Task Receiving_the_rest_marks_order_received()
    {
        var (order, product, storage) = await CreateOrderAsync(10);
        Assert.True((await RunAsync(s => s.ReceiveAsync(order.Id, Receipt(order, 4), User))).Success);
        var partial = await LoadAsync(order.Id);

        var result = await RunAsync(s => s.ReceiveAsync(order.Id, Receipt(partial, 6, invoice: "NF-2"), User));

        Assert.True(result.Success);
        var saved = await LoadAsync(order.Id);
        Assert.Equal(PurchaseOrderStatus.Received, saved.Status);
        Assert.Equal(2, saved.Receipts.Count);
        Assert.Equal(10, await GetBalanceAsync(_database, product.Id, storage.Id));
        await AssertLedgerClosesAsync(_database, product.Id);
    }

    // ---------- Recebimento maior que o pedido ----------

    [Fact]
    public async Task Receipt_above_pending_is_refused_and_writes_nothing()
    {
        var (order, product, storage) = await CreateOrderAsync(10);

        var result = await RunAsync(s => s.ReceiveAsync(order.Id, Receipt(order, 11), User));

        Assert.False(result.Success);
        Assert.False(result.IsConflict);
        Assert.StartsWith("Recebimento maior que o pedido para Produto de teste: pendente 10, informado 11.", result.Error);
        Assert.Equal(PurchaseOrderStatus.Open, (await LoadAsync(order.Id)).Status);
        Assert.Null(await GetBalanceAsync(_database, product.Id, storage.Id));
    }

    [Fact]
    public async Task Fully_received_order_refuses_new_receipts()
    {
        var (order, _, _) = await CreateOrderAsync(5);
        Assert.True((await RunAsync(s => s.ReceiveAsync(order.Id, Receipt(order, 5), User))).Success);
        var received = await LoadAsync(order.Id);

        var result = await RunAsync(s => s.ReceiveAsync(order.Id, Receipt(received, 1, invoice: "NF-2"), User));

        Assert.False(result.Success);
        Assert.Contains("já foi recebido por completo", result.Error);
    }

    // ---------- Cancelamento ----------

    [Fact]
    public async Task Cancelling_after_partial_receipt_keeps_what_arrived()
    {
        var (order, product, storage) = await CreateOrderAsync(10);
        Assert.True((await RunAsync(s => s.ReceiveAsync(order.Id, Receipt(order, 3), User))).Success);
        var partial = await LoadAsync(order.Id);

        var result = await RunAsync(s => s.CancelAsync(order.Id, new CancelPurchaseOrderDTO("Fornecedor sem estoque", partial.RowVersion), User));

        Assert.True(result.Success);
        var saved = await LoadAsync(order.Id);
        Assert.Equal(PurchaseOrderStatus.Cancelled, saved.Status);
        Assert.Equal("Fornecedor sem estoque", saved.CancelReason);
        Assert.Equal(User, saved.CancelledBy);
        Assert.Equal(3, saved.Items[0].ReceivedQuantity);
        Assert.Equal(3, await GetBalanceAsync(_database, product.Id, storage.Id));
    }

    [Fact]
    public async Task Cancelled_order_refuses_receipts()
    {
        var (order, product, storage) = await CreateOrderAsync();
        Assert.True((await RunAsync(s => s.CancelAsync(order.Id, new CancelPurchaseOrderDTO("Pedido duplicado", order.RowVersion), User))).Success);
        var cancelled = await LoadAsync(order.Id);

        var result = await RunAsync(s => s.ReceiveAsync(order.Id, Receipt(cancelled, 1), User));

        Assert.False(result.Success);
        Assert.Contains("está cancelado", result.Error);
        Assert.Null(await GetBalanceAsync(_database, product.Id, storage.Id));
    }

    [Fact]
    public async Task Fully_received_order_cannot_be_cancelled()
    {
        var (order, _, _) = await CreateOrderAsync(5);
        Assert.True((await RunAsync(s => s.ReceiveAsync(order.Id, Receipt(order, 5), User))).Success);
        var received = await LoadAsync(order.Id);

        var result = await RunAsync(s => s.CancelAsync(order.Id, new CancelPurchaseOrderDTO("Tarde demais", received.RowVersion), User));

        Assert.False(result.Success);
        Assert.Contains("não há pendente para cancelar", result.Error);
        Assert.Equal(PurchaseOrderStatus.Received, (await LoadAsync(order.Id)).Status);
    }

    // ---------- Preço da nota diferente do pedido ----------

    [Fact]
    public async Task Invoice_price_different_from_order_requires_a_reason()
    {
        var (order, product, storage) = await CreateOrderAsync(10);

        var refused = await RunAsync(s => s.ReceiveAsync(order.Id, Receipt(order, 10, unitPrice: 13.10m), User));

        Assert.False(refused.Success);
        Assert.Contains("Produto de teste (pedido R$ 12,50, nota R$ 13,10)", refused.Error);
        Assert.Null(await GetBalanceAsync(_database, product.Id, storage.Id));

        var accepted = await RunAsync(s => s.ReceiveAsync(order.Id,
            Receipt(order, 10, unitPrice: 13.10m, divergenceReason: "Reajuste de tabela do fornecedor"), User));

        Assert.True(accepted.Success);
        var receipt = Assert.Single((await LoadAsync(order.Id)).Receipts);
        Assert.Equal("Reajuste de tabela do fornecedor", receipt.PriceDivergenceReason);
        Assert.Equal(13.10m, Assert.Single(receipt.Items).UnitPrice);
    }

    [Fact]
    public async Task Same_price_ignores_the_divergence_reason()
    {
        var (order, _, _) = await CreateOrderAsync();

        Assert.True((await RunAsync(s => s.ReceiveAsync(order.Id, Receipt(order, 1, divergenceReason: "Não se aplica"), User))).Success);

        Assert.Null(Assert.Single((await LoadAsync(order.Id)).Receipts).PriceDivergenceReason);
    }

    // ---------- Nota duplicada, concorrência e falha no meio ----------

    [Fact]
    public async Task Same_invoice_from_same_supplier_is_refused()
    {
        var (order, _, _) = await CreateOrderAsync(10);
        Assert.True((await RunAsync(s => s.ReceiveAsync(order.Id, Receipt(order, 2, invoice: "NF-DUP"), User))).Success);
        var partial = await LoadAsync(order.Id);

        var result = await RunAsync(s => s.ReceiveAsync(order.Id, Receipt(partial, 2, invoice: "NF-DUP"), User));

        Assert.False(result.Success);
        Assert.StartsWith("A nota NF-DUP deste fornecedor já foi lançada", result.Error);
    }

    // Duas pessoas abrem o pedido e lançam a mesma entrega: só a primeira entra no estoque.
    [Fact]
    public async Task Receipt_with_stale_RowVersion_is_a_conflict_and_writes_nothing()
    {
        var (order, product, storage) = await CreateOrderAsync(10);
        Assert.True((await RunAsync(s => s.ReceiveAsync(order.Id, Receipt(order, 4, invoice: "NF-A"), User))).Success);

        var result = await RunAsync(s => s.ReceiveAsync(order.Id, Receipt(order, 4, invoice: "NF-B"), User));

        Assert.False(result.Success);
        Assert.True(result.IsConflict);
        Assert.Equal(4, await GetBalanceAsync(_database, product.Id, storage.Id));
        Assert.Single((await LoadAsync(order.Id)).Receipts);
    }

    // Se um passo depois do SaveChanges falhar (ex.: contas a pagar), nada do recebimento fica gravado.
    [Fact]
    public async Task Failure_after_saving_rolls_back_the_whole_receipt()
    {
        var (order, product, storage) = await CreateOrderAsync(10);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            RunAsync(s => s.ReceiveAsync(order.Id, Receipt(order, 4), User), new FailingListener()));

        var saved = await LoadAsync(order.Id);
        Assert.Equal(PurchaseOrderStatus.Open, saved.Status);
        Assert.Equal(0, saved.Items[0].ReceivedQuantity);
        Assert.Empty(saved.Receipts);
        Assert.Null(await GetBalanceAsync(_database, product.Id, storage.Id));
        Assert.Empty(await GetMovementsAsync(_database, product.Id, storage.Id));
    }

    [Fact]
    public async Task Inactive_product_cannot_be_received()
    {
        var (order, product, storage) = await CreateOrderAsync();
        await using (var db = _database.CreateContext())
        {
            await db.Products.Where(p => p.Id == product.Id).ExecuteUpdateAsync(p => p.SetProperty(x => x.IsActive, false));
        }

        var result = await RunAsync(s => s.ReceiveAsync(order.Id, Receipt(order, 1), User));

        Assert.False(result.Success);
        Assert.Contains("está inativo", result.Error);
        Assert.Null(await GetBalanceAsync(_database, product.Id, storage.Id));
    }
}
