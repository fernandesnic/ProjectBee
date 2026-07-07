namespace ProjectBee.Interfaces;

public interface IProductDTO
{
    string Name { get; init; }
    decimal Price { get; init; }
    string SKU { get; init; }
}