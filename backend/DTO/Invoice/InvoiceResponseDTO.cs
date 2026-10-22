namespace ProjectBee.DTO.Invoice;

public record InvoiceItemResponseDTO(
    Guid ProductId,
    string ProductName,
    string ProductSku,
    string Batch,
    int Quantity,
    decimal UnitPrice);

public record InvoiceResponseDTO(
    Guid Id,
    int Number,
    Guid SalesOrderId,
    int SalesOrderNumber,
    Guid CustomerId,
    string CustomerName,
    string CustomerDocument,
    string StorageName,
    decimal Total,
    string CreatedBy,
    DateTime CreatedAt,
    List<InvoiceItemResponseDTO> Items);
