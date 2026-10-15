namespace ProjectBee.DTO.Movement;

public record AdjustmentDTO(Guid ProductId, Guid StorageId, string Batch, int CountedQuantity, string Reason);
