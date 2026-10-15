using ProjectBee.Interfaces;

namespace ProjectBee.DTO.Product;

public record CreateProductDTO(string Name, string SKU, string Desc, decimal Price) : IProductDTO;
