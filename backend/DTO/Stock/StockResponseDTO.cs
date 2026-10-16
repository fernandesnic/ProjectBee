namespace ProjectBee.DTO.Stock;

public record StockResponseDTO(Guid ProductId, string ProductName, decimal ProductPrice, Guid StorageId, string StorageName, string StorageAddress, int Balance, string Batch, bool ProductDeleted, bool StorageDeleted);
