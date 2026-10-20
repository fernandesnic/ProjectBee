namespace ProjectBee.DTO.Purchase;

// UnitPrice é o preço da nota. Se diferir do pedido, PriceDivergenceReason é obrigatório.
public record ReceiptItemDTO(Guid PurchaseOrderItemId, string Batch, int Quantity, decimal UnitPrice);

// RowVersion: a versão do pedido lida antes de receber. Se outro recebimento ou um cancelamento
// gravou no meio, a API devolve 409 em vez de lançar a mesma mercadoria duas vezes.
public record ReceivePurchaseOrderDTO(string InvoiceNumber, string? PriceDivergenceReason, List<ReceiptItemDTO> Items, byte[]? RowVersion);
