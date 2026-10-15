public record MovementResponseDTO(
    Guid Id,
    string Type,
    Guid ProductId,
    string ProductName,
    Guid StorageId,
    string StorageName,
    string Batch,
    int Quantity,
    string? Reason,
    Guid? TransferId,
    string CreatedBy,
    DateTime CreatedAt);