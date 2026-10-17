namespace ProjectBee.DTO.Product;

public record ProductResponseDTO(Guid Id, string Name, string SKU, string Desc, decimal Price, bool IsActive, byte[] RowVersion);
