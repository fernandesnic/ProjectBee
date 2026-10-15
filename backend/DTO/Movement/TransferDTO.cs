namespace ProjectBee.DTO.Movement;

public record TransferDTO(Guid ProductId, Guid FromStorageId, Guid ToStorageId, string Batch, int Quantity);
