namespace ProjectBee.DTO.Purchase;

public record PurchaseOrderSummaryDTO(
    Guid Id,
    int Number,
    string Status,
    Guid SupplierId,
    string SupplierName,
    Guid StorageId,
    string StorageName,
    decimal Total,
    int OrderedUnits,
    int ReceivedUnits,
    DateTime CreatedAt);

public record PurchaseOrderItemResponseDTO(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string ProductSku,
    int Quantity,
    int ReceivedQuantity,
    decimal UnitPrice);

public record PurchaseReceiptItemResponseDTO(
    Guid PurchaseOrderItemId,
    string ProductName,
    string Batch,
    int Quantity,
    decimal UnitPrice,
    decimal OrderedUnitPrice);

public record PurchaseReceiptResponseDTO(
    Guid Id,
    string InvoiceNumber,
    string? PriceDivergenceReason,
    string CreatedBy,
    DateTime CreatedAt,
    List<PurchaseReceiptItemResponseDTO> Items);

public record PurchaseOrderDetailDTO(
    Guid Id,
    int Number,
    string Status,
    Guid SupplierId,
    string SupplierName,
    string SupplierDocument,
    Guid StorageId,
    string StorageName,
    string? Notes,
    string CreatedBy,
    DateTime CreatedAt,
    string? CancelledBy,
    DateTime? CancelledAt,
    string? CancelReason,
    byte[] RowVersion,
    List<PurchaseOrderItemResponseDTO> Items,
    List<PurchaseReceiptResponseDTO> Receipts);
