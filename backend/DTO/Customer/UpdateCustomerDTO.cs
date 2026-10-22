using ProjectBee.Interfaces;

namespace ProjectBee.DTO.Customer;

// RowVersion: a versão lida antes de editar. Se outra edição gravou no meio, o PUT devolve 409.
public record UpdateCustomerDTO(string Name, string Email, string Phone, bool? IsActive, byte[]? RowVersion) : ICustomerDTO;
