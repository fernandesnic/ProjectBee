using System.Globalization;
using Microsoft.EntityFrameworkCore;
using ProjectBee.Data;
using ProjectBee.DTO.Purchase;
using ProjectBee.Models;

namespace ProjectBee.Services;

public record PurchaseResult(bool Success, Guid? OrderId = null, string? Error = null, bool IsConflict = false, bool IsNotFound = false)
    : IOperationResult
{
    public static PurchaseResult Ok(Guid orderId) => new(true, orderId);
    public static PurchaseResult Fail(string error) => new(false, Error: error);
    public static PurchaseResult NotFound() => new(false, Error: "Pedido de compra não encontrado.", IsNotFound: true);
    public static PurchaseResult Conflict() => new(false,
        Error: "O pedido foi alterado por outra operação ao mesmo tempo (outro recebimento ou um cancelamento). Recarregue o pedido e confira o pendente antes de tentar de novo.",
        IsConflict: true);
}

// Ponto de extensão para Contas a Pagar: cada nota recebida vai virar um título. O listener roda
// dentro da mesma unidade de trabalho do recebimento, então se ele falhar, a nota, as entradas
// no estoque e a mudança de status são desfeitas juntas. Hoje nenhum está registrado.
public interface IPurchaseReceiptListener
{
    Task OnReceiptRegisteredAsync(PurchaseOrder order, PurchaseReceipt receipt);
}

public class PurchaseService
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    private readonly AppDbContext _db;
    private readonly UnitOfWork _unitOfWork;
    private readonly StockMovementService _stock;
    private readonly IEnumerable<IPurchaseReceiptListener> _receiptListeners;

    public PurchaseService(
        AppDbContext db,
        UnitOfWork unitOfWork,
        StockMovementService stock,
        IEnumerable<IPurchaseReceiptListener> receiptListeners)
    {
        _db = db;
        _unitOfWork = unitOfWork;
        _stock = stock;
        _receiptListeners = receiptListeners;
    }

    public static string Code(int number) => $"PC-{number:D6}";

    public Task<PurchaseResult> CreateOrderAsync(CreatePurchaseOrderDTO dto, string user) =>
        RunAsync(async () =>
        {
            var now = DateTime.UtcNow;
            var order = new PurchaseOrder
            {
                SupplierId = dto.SupplierId,
                StorageId = dto.StorageId,
                Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
                Status = PurchaseOrderStatus.Open,
                CreatedBy = user,
                CreatedAt = now,
                UpdatedAt = now,
                Items = dto.Items.Select(i => new PurchaseOrderItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            };

            _db.PurchaseOrders.Add(order);
            await _db.SaveChangesAsync();
            return PurchaseResult.Ok(order.Id);
        });

    public Task<PurchaseResult> ReceiveAsync(Guid orderId, ReceivePurchaseOrderDTO dto, string user) =>
        RunAsync(async () =>
        {
            var order = await LoadOrderAsync(orderId, dto.RowVersion!);
            if (order == null)
                return PurchaseResult.NotFound();

            var code = Code(order.Number);

            if (order.Status == PurchaseOrderStatus.Cancelled)
                return PurchaseResult.Fail($"O pedido {code} está cancelado e não aceita recebimentos.");

            if (order.Status == PurchaseOrderStatus.Received)
                return PurchaseResult.Fail($"O pedido {code} já foi recebido por completo. Mercadoria além do pedido precisa de um pedido novo.");

            if (!order.Storage.IsActive || order.Storage.DeletedAt != null)
                return PurchaseResult.Fail($"O armazém {order.Storage.Name} está inativo e não pode receber mercadoria. Reative-o antes de lançar a nota.");

            var invoice = dto.InvoiceNumber.Trim();
            // A mesma nota do mesmo fornecedor lançada duas vezes duplicaria o estoque e, no futuro, o título a pagar.
            var duplicatedIn = await _db.PurchaseReceipts
                .Where(r => r.InvoiceNumber == invoice && r.PurchaseOrder.SupplierId == order.SupplierId)
                .Select(r => (int?)r.PurchaseOrder.Number)
                .FirstOrDefaultAsync();
            if (duplicatedIn != null)
                return PurchaseResult.Fail($"A nota {invoice} deste fornecedor já foi lançada no pedido {Code(duplicatedIn.Value)}.");

            var itemsById = order.Items.ToDictionary(i => i.Id);
            var unknown = dto.Items.FirstOrDefault(l => !itemsById.ContainsKey(l.PurchaseOrderItemId));
            if (unknown != null)
                return PurchaseResult.Fail($"O item {unknown.PurchaseOrderItemId} não pertence ao pedido {code}.");

            foreach (var group in dto.Items.GroupBy(l => l.PurchaseOrderItemId))
            {
                var item = itemsById[group.Key];
                var pending = item.Quantity - item.ReceivedQuantity;
                var received = group.Sum(l => l.Quantity);

                if (!item.Product.IsActive || item.Product.DeletedAt != null)
                    return PurchaseResult.Fail($"O produto {item.Product.Name} está inativo e não pode dar entrada no estoque.");

                // Recebimento maior que o pedido é recusado: o excedente volta com o fornecedor ou vira um pedido novo.
                if (received > pending)
                    return PurchaseResult.Fail(
                        $"Recebimento maior que o pedido para {item.Product.Name}: pendente {pending}, informado {received}. " +
                        "Recuse o excedente ou faça um novo pedido para ele.");
            }

            var divergent = dto.Items
                .Where(l => l.UnitPrice != itemsById[l.PurchaseOrderItemId].UnitPrice)
                .Select(l => $"{itemsById[l.PurchaseOrderItemId].Product.Name} (pedido {Money(itemsById[l.PurchaseOrderItemId].UnitPrice)}, nota {Money(l.UnitPrice)})")
                .Distinct()
                .ToList();
            if (divergent.Count > 0 && string.IsNullOrWhiteSpace(dto.PriceDivergenceReason))
                return PurchaseResult.Fail(
                    $"O preço da nota difere do pedido em: {string.Join("; ", divergent)}. " +
                    "Informe o motivo da divergência para confirmar o recebimento.");

            var receipt = new PurchaseReceipt
            {
                PurchaseOrderId = order.Id,
                InvoiceNumber = invoice,
                PriceDivergenceReason = divergent.Count > 0 ? dto.PriceDivergenceReason!.Trim() : null,
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow,
                Items = dto.Items.Select(l => new PurchaseReceiptItem
                {
                    PurchaseOrderItemId = l.PurchaseOrderItemId,
                    Batch = l.Batch,
                    Quantity = l.Quantity,
                    UnitPrice = l.UnitPrice
                }).ToList()
            };
            _db.PurchaseReceipts.Add(receipt);

            foreach (var line in dto.Items)
            {
                var item = itemsById[line.PurchaseOrderItemId];
                item.ReceivedQuantity += line.Quantity;
                await _stock.StageEntryAsync(item.ProductId, order.StorageId, line.Batch, line.Quantity, user,
                    MovementType.PurchaseReceipt, receipt.Id);
            }

            // Recebimento menor que o pendente deixa o pedido Parcial, aguardando as próximas notas.
            order.Status = order.Items.All(i => i.ReceivedQuantity >= i.Quantity)
                ? PurchaseOrderStatus.Received
                : PurchaseOrderStatus.Partial;
            order.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            foreach (var listener in _receiptListeners)
                await listener.OnReceiptRegisteredAsync(order, receipt);
            await _db.SaveChangesAsync();

            return PurchaseResult.Ok(order.Id);
        });

    // Cancelar depois de receber parte cancela só o pendente: o que chegou continua no estoque,
    // e as notas já lançadas (e os títulos que sairão delas) seguem valendo.
    public Task<PurchaseResult> CancelAsync(Guid orderId, CancelPurchaseOrderDTO dto, string user) =>
        RunAsync(async () =>
        {
            var order = await LoadOrderAsync(orderId, dto.RowVersion!);
            if (order == null)
                return PurchaseResult.NotFound();

            var code = Code(order.Number);

            if (order.Status == PurchaseOrderStatus.Cancelled)
                return PurchaseResult.Fail($"O pedido {code} já está cancelado.");

            if (order.Status == PurchaseOrderStatus.Received)
                return PurchaseResult.Fail($"O pedido {code} já foi recebido por completo: não há pendente para cancelar.");

            var now = DateTime.UtcNow;
            order.Status = PurchaseOrderStatus.Cancelled;
            order.CancelReason = dto.Reason.Trim();
            order.CancelledBy = user;
            order.CancelledAt = now;
            order.UpdatedAt = now;

            await _db.SaveChangesAsync();
            return PurchaseResult.Ok(order.Id);
        });

    // IgnoreQueryFilters: um produto ou armazém excluído não pode sumir do pedido (o Include filtraria
    // a linha). A versão enviada vai no WHERE do UPDATE: se outra operação gravou depois da leitura
    // do cliente, o SaveChanges lança DbUpdateConcurrencyException e nada é gravado.
    private async Task<PurchaseOrder?> LoadOrderAsync(Guid orderId, byte[] rowVersion)
    {
        var order = await _db.PurchaseOrders
            .IgnoreQueryFilters()
            .Include(o => o.Storage)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
            return null;

        if (!order.RowVersion.SequenceEqual(rowVersion))
            throw new DbUpdateConcurrencyException();
        _db.Entry(order).Property(o => o.RowVersion).OriginalValue = rowVersion;

        return order;
    }

    private async Task<PurchaseResult> RunAsync(Func<Task<PurchaseResult>> work)
    {
        try
        {
            return await _unitOfWork.ExecuteAsync(work);
        }
        catch (DbUpdateConcurrencyException)
        {
            return PurchaseResult.Conflict();
        }
    }

    // Sem "C": no Linux (ICU) o pt-BR separa "R$" com espaço não separável, e a mensagem mudaria por ambiente.
    private static string Money(decimal value) => $"R$ {value.ToString("N2", PtBr)}";
}
