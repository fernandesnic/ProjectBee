namespace ProjectBee.DTO.Storage;

public record StorageResponseDTO(Guid Id, string Name, string IdNumber, string AddressNumber, string AddressStreet, string AddressCity, bool IsActive, byte[] RowVersion);
