using ProjectBee.Interfaces;

namespace ProjectBee.DTO.Storage;

public record CreateStorageDTO(string IdNumber, string Name, string AddressNumber, string AddressStreet, string AddressCity) : IStorageDTO;
