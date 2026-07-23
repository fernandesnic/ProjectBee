namespace ProjectBee.Interfaces;

public interface IStockDTO
{
    Guid ProductId { get; init; }
    Product Product { get; init; } = null!;
    Guid StorageId { get; init; }
    Storage Storage { get; init; } = null!;
    int Balance { get; init; }
    string Batch { get; init; } = string.Empty;
}