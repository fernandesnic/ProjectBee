namespace ProjectBee.DTO.Sales;

// Sem preço: o item sai pelo preço do produto no momento do pedido.
public record SalesOrderItemDTO(Guid ProductId, int Quantity);

// PaymentMethod: Boleto, Pix ou CreditCard. Por enquanto só Boleto é aceito.
public record CreateSalesOrderDTO(Guid CustomerId, Guid StorageId, string PaymentMethod, string? Notes, List<SalesOrderItemDTO> Items);
