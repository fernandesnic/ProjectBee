using ProjectBee.Interfaces;

namespace ProjectBee.DTO.Storage;

public record UpdateStorageDTO(string Name, string AddressNumber, string AddressStreet, string AddressCity, bool? IsActive) : IStorageDTO;
