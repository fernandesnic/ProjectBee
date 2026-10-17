using ProjectBee.Interfaces;

namespace ProjectBee.DTO.Storage;

// RowVersion: a versão lida antes de editar. Se outra edição gravou no meio, o PUT devolve 409.
public record UpdateStorageDTO(string Name, string AddressNumber, string AddressStreet, string AddressCity, bool? IsActive, byte[]? RowVersion) : IStorageDTO;
