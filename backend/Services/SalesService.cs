using System.Globalization;
using Microsoft.EntityFrameworkCore;
using ProjectBee.Data;
using ProjectBee.DTO.Invoice;
using ProjectBee.DTO.Sales;
using ProjectBee.Models;

namespace ProjectBee.Services;

// Id é o do pedido (criação e pagamento) ou o da nota (faturamento).
public record SalesResult(bool Success, Guid? Id = null, string? Error = null, bool IsConflict = false, bool IsNotFound = false)
    : IOperationResult
{
    public static SalesResult Ok(Guid id) => new(true, id);
    public static SalesResult Fail(string error) => new(false, Error: error);
    public static SalesResult NotFound() => new(false, Error: "Pedido de venda não encontrado.", IsNotFound: true);
    public static SalesResult Conflict() => new(false,
        Error: "O pedido ou o saldo do estoque foi alterado por outra operação ao mesmo tempo. Recarregue o pedido e confira antes de tentar de novo.",
        IsConflict: true);
}

public class SalesService
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    // Pedidos que ainda vão tirar mercadoria do estoque: o saldo deles já está comprometido.
    private static readonly SalesOrderStatus[] Reserving = [SalesOrderStatus.AwaitingPayment, SalesOrderStatus.Paid];

    private const int BoletoDueDays = 3;

    private readonly AppDbContext _db;
    private readonly UnitOfWork _unitOfWork;
    private readonly StockMovementService _stock;

    public SalesService(AppDbContext db, UnitOfWork unitOfWork, StockMovementService stock)
    {
        _db = db;
        _unitOfWork = unitOfWork;
        _stock = stock;
    }

    public static string Code(int number) => $"PV-{number:D6}";
    public static string InvoiceCode(int number) => $"NF-{number:D6}";

    // O pedido nasce com o boleto emitido. O número do boleto vem do número do pedido, que só
    // existe depois do primeiro SaveChanges: por isso os dois saves, dentro da mesma transação.
    public Task<SalesResult> CreateOrderAsync(CreateSalesOrderDTO dto, string user) =>
        RunAsync(async () =>
        {
            var productIds = dto.Items.Select(i => i.ProductId).ToList();
            var products = await _db.Products
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            var storageName = await _db.Storages
                .Where(s => s.Id == dto.StorageId && s.IsActive)
                .Select(s => s.Name)
                .FirstOrDefaultAsync();

            // O validator já conferiu, mas fora da transação: um cadastro excluído ou desativado no
            // meio vira 422 aqui, em vez de estourar no dicionário ou no FirstAsync.
            if (storageName == null || productIds.Any(id => !products.TryGetValue(id, out var p) || !p.IsActive))
                return SalesResult.Fail("O armazém ou um dos produtos do pedido foi excluído ou desativado. Recarregue e confira o pedido.");

            // Disponível = saldo físico do armazém menos o que outros pedidos ainda não faturados já
            // venderam. Sem descontar a reserva, o mesmo pote seria vendido duas vezes.
            foreach (var item in dto.Items)
            {
                var available = await AvailableAsync(item.ProductId, dto.StorageId);
                if (item.Quantity > available)
                    return SalesResult.Fail(
                        $"Saldo insuficiente de {products[item.ProductId].Name} em {storageName}: " +
                        $"disponível {available}, pedido {item.Quantity}.");
            }

            var total = dto.Items.Sum(i => i.Quantity * products[i.ProductId].Price);
            if (total > BoletoGenerator.MaxAmount)
                return SalesResult.Fail($"O total do pedido ({Money(total)}) passa do limite de um boleto ({Money(BoletoGenerator.MaxAmount)}). Divida a venda em mais de um pedido.");

            var now = DateTime.UtcNow;
            var order = new SalesOrder
            {
                CustomerId = dto.CustomerId,
                StorageId = dto.StorageId,
                PaymentMethod = Enum.Parse<PaymentMethod>(dto.PaymentMethod, ignoreCase: true),
                Notes = string.IsNullOrWhiteSpace(dto.Notes) ? null : dto.Notes.Trim(),
                Status = SalesOrderStatus.AwaitingPayment,
                CreatedBy = user,
                CreatedAt = now,
                UpdatedAt = now,
                Items = dto.Items.Select(i => new SalesOrderItem
                {
                    ProductId = i.ProductId,
                    Quantity = i.Quantity,
                    UnitPrice = products[i.ProductId].Price
                }).ToList()
            };

            _db.SalesOrders.Add(order);
            await _db.SaveChangesAsync();

            var ourNumber = BoletoGenerator.OurNumber(order.Number);
            var dueDate = BrazilTime.DateOf(now).AddDays(BoletoDueDays);
            _db.Boletos.Add(new Boleto
            {
                SalesOrderId = order.Id,
                OurNumber = ourNumber,
                DigitableLine = BoletoGenerator.DigitableLine(ourNumber, total, dueDate),
                Amount = total,
                DueDate = dueDate,
                CreatedAt = now
            });
            await _db.SaveChangesAsync();

            return SalesResult.Ok(order.Id);
        });

    // Simula o retorno do banco: o boleto é dado como pago e o pedido fica liberado para faturar.
    public Task<SalesResult> PayBoletoAsync(Guid orderId, PayBoletoDTO dto, string user) =>
        RunAsync(async () =>
        {
            var order = await LoadOrderAsync(orderId, dto.RowVersion!);
            if (order == null)
                return SalesResult.NotFound();

            var code = Code(order.Number);

            if (order.Boleto == null)
                return SalesResult.Fail($"O pedido {code} não tem boleto.");

            if (order.Status != SalesOrderStatus.AwaitingPayment || order.Boleto.Status == BoletoStatus.Paid)
                return SalesResult.Fail($"O boleto do pedido {code} já foi pago.");

            var now = DateTime.UtcNow;
            order.Boleto.Status = BoletoStatus.Paid;
            order.Boleto.PaidBy = user;
            order.Boleto.PaidAt = now;
            order.Status = SalesOrderStatus.Paid;
            order.UpdatedAt = now;

            await _db.SaveChangesAsync();
            return SalesResult.Ok(order.Id);
        });

    // Nota, saídas do estoque e status do pedido entram juntos ou não entram. A saída segue o
    // FIFO: o lote que entrou primeiro no armazém sai primeiro, e um item pode sair de vários lotes.
    public Task<SalesResult> InvoiceAsync(CreateInvoiceDTO dto, string user) =>
        RunAsync(async () =>
        {
            var order = await LoadOrderAsync(dto.SalesOrderId, dto.RowVersion!);
            if (order == null)
                return SalesResult.NotFound();

            var code = Code(order.Number);

            if (order.Status == SalesOrderStatus.AwaitingPayment)
                return SalesResult.Fail($"O boleto do pedido {code} ainda não foi pago. A nota só sai depois do pagamento.");

            if (order.Status == SalesOrderStatus.Invoiced)
                return SalesResult.Fail($"O pedido {code} já foi faturado.");

            if (!order.Storage.IsActive || order.Storage.DeletedAt != null)
                return SalesResult.Fail($"O armazém {order.Storage.Name} está inativo e não pode expedir mercadoria. Reative-o antes de emitir a nota.");

            // O pedido é gravado primeiro, com o RowVersion lido no WHERE: duas emissões simultâneas
            // disputam essa linha, e a segunda espera o lock e sai com 409. Se a nota fosse inserida
            // antes, a segunda bateria no índice único da nota e terminaria em 500.
            order.Status = SalesOrderStatus.Invoiced;
            order.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            var invoice = new SalesInvoice
            {
                SalesOrderId = order.Id,
                CreatedBy = user,
                CreatedAt = DateTime.UtcNow
            };

            foreach (var item in order.Items)
            {
                var batches = await FifoBalancesAsync(item.ProductId, order.StorageId);
                var available = batches.Sum(b => b.Balance);
                if (available < item.Quantity)
                    return SalesResult.Fail(
                        $"Saldo insuficiente de {item.Product.Name} em {order.Storage.Name}: " +
                        $"disponível {available}, pedido {item.Quantity}. Dê entrada na mercadoria antes de faturar.");

                var remaining = item.Quantity;
                foreach (var balance in batches)
                {
                    if (remaining == 0)
                        break;

                    var taken = Math.Min(remaining, balance.Balance);
                    invoice.Items.Add(new SalesInvoiceItem
                    {
                        ProductId = item.ProductId,
                        Batch = balance.Batch,
                        Quantity = taken,
                        UnitPrice = item.UnitPrice
                    });
                    await _stock.StageExitAsync(item.ProductId, order.StorageId, balance.Batch, taken, user,
                        MovementType.SalesInvoice, invoice.Id);
                    remaining -= taken;
                }
            }

            _db.SalesInvoices.Add(invoice);
            await _db.SaveChangesAsync();
            return SalesResult.Ok(invoice.Id);
        });

    private async Task<int> AvailableAsync(Guid productId, Guid storageId)
    {
        var physical = await _db.StockBalances
            .Where(b => b.ProductId == productId && b.StorageId == storageId)
            .SumAsync(b => b.Balance);

        var reserved = await _db.SalesOrders
            .Where(o => o.StorageId == storageId && Reserving.Contains(o.Status))
            .SelectMany(o => o.Items)
            .Where(i => i.ProductId == productId)
            .SumAsync(i => i.Quantity);

        return physical - reserved;
    }

    // Lotes com saldo, do que entrou primeiro no armazém para o mais recente. Os saldos voltam
    // rastreados: a saída decrementa essas mesmas instâncias, e o RowVersion de cada uma vai no UPDATE.
    private async Task<List<StockBalance>> FifoBalancesAsync(Guid productId, Guid storageId)
    {
        var balances = await _db.StockBalances
            .Where(b => b.ProductId == productId && b.StorageId == storageId && b.Balance > 0)
            .ToListAsync();

        var firstEntry = await _db.StockMovements
            .Where(m => m.ProductId == productId && m.StorageId == storageId && m.Quantity > 0)
            .GroupBy(m => m.Batch)
            .Select(g => new { Batch = g.Key, First = g.Min(m => m.CreatedAt) })
            .ToDictionaryAsync(x => x.Batch, x => x.First);

        return balances
            .OrderBy(b => firstEntry.GetValueOrDefault(b.Batch, DateTime.MaxValue))
            .ThenBy(b => b.Batch, StringComparer.Ordinal)
            .ToList();
    }

    // Mesmo cuidado do pedido de compra: a versão enviada vai no WHERE do UPDATE, e produto ou
    // armazém excluídos não podem sumir do pedido (o Include filtraria a linha).
    private async Task<SalesOrder?> LoadOrderAsync(Guid orderId, byte[] rowVersion)
    {
        var order = await _db.SalesOrders
            .IgnoreQueryFilters()
            .Include(o => o.Storage)
            .Include(o => o.Boleto)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId);

        if (order == null)
            return null;

        if (!order.RowVersion.SequenceEqual(rowVersion))
            throw new DbUpdateConcurrencyException();
        _db.Entry(order).Property(o => o.RowVersion).OriginalValue = rowVersion;

        return order;
    }

    private async Task<SalesResult> RunAsync(Func<Task<SalesResult>> work)
    {
        try
        {
            return await _unitOfWork.ExecuteAsync(work);
        }
        catch (DbUpdateConcurrencyException)
        {
            return SalesResult.Conflict();
        }
    }

    // Sem "C": no Linux (ICU) o pt-BR separa "R$" com espaço não separável, e a mensagem mudaria por ambiente.
    private static string Money(decimal value) => $"R$ {value.ToString("N2", PtBr)}";
}
