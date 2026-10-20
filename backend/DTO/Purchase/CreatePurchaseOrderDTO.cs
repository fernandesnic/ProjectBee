namespace ProjectBee.DTO.Purchase;

public record PurchaseOrderItemDTO(Guid ProductId, int Quantity, decimal UnitPrice);

public record CreatePurchaseOrderDTO(Guid SupplierId, Guid StorageId, string? Notes, List<PurchaseOrderItemDTO> Items);
