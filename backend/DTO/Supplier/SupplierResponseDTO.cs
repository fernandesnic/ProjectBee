namespace ProjectBee.DTO.Supplier;

public record SupplierResponseDTO(Guid Id, string Name, string Document, string Email, string Phone, bool IsActive, byte[] RowVersion);
