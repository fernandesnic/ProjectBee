namespace ProjectBee.DTO.Sales;

public record SalesOrderSummaryDTO(
    Guid Id,
    int Number,
    string Status,
    string PaymentMethod,
    Guid CustomerId,
    string CustomerName,
    Guid StorageId,
    string StorageName,
    decimal Total,
    int Units,
    DateTime CreatedAt);

public record SalesOrderItemResponseDTO(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string ProductSku,
    int Quantity,
    decimal UnitPrice);

public record BoletoResponseDTO(
    Guid Id,
    string OurNumber,
    string DigitableLine,
    decimal Amount,
    DateOnly DueDate,
    string Status,
    DateTime CreatedAt,
    string? PaidBy,
    DateTime? PaidAt);

public record SalesOrderInvoiceDTO(Guid Id, int Number, DateTime CreatedAt);

public record SalesOrderDetailDTO(
    Guid Id,
    int Number,
    string Status,
    string PaymentMethod,
    Guid CustomerId,
    string CustomerName,
    string CustomerDocument,
    Guid StorageId,
    string StorageName,
    string? Notes,
    string CreatedBy,
    DateTime CreatedAt,
    byte[] RowVersion,
    List<SalesOrderItemResponseDTO> Items,
    BoletoResponseDTO? Boleto,
    SalesOrderInvoiceDTO? Invoice);
