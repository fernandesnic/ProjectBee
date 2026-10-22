namespace ProjectBee.DTO.Invoice;

// RowVersion: a versão do pedido lida antes de faturar. Se outra pessoa faturou no meio,
// a API devolve 409 em vez de baixar a mesma mercadoria duas vezes.
public record CreateInvoiceDTO(Guid SalesOrderId, byte[]? RowVersion);
