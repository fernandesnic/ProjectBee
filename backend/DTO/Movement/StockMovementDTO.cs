namespace ProjectBee.DTO.Movement;

public record StockMovementDTO(Guid ProductId, Guid StorageId, string Batch, int Quantity);
