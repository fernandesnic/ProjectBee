using ProjectBee.Interfaces;

namespace ProjectBee.DTO.Product;

// RowVersion: a versão lida antes de editar. Se outra edição gravou no meio, o PUT devolve 409.
public record UpdateProductDTO(string Name, string Desc, decimal Price, bool? IsActive, byte[]? RowVersion) : IProductDTO;
