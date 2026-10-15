using ProjectBee.Interfaces;

namespace ProjectBee.DTO.Product;

public record UpdateProductDTO(string Name, string Desc, decimal Price, bool? IsActive) : IProductDTO;
