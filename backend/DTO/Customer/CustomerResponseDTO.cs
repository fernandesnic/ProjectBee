namespace ProjectBee.DTO.Customer;

public record CustomerResponseDTO(Guid Id, string Name, string Document, string Email, string Phone, bool IsActive, byte[] RowVersion);
