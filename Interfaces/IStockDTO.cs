namespace ProjectBee.Interfaces;

public interface IStockDTO
{
    Guid ProductId { get; init; }
    Guid StorageId { get; init; }
    int Balance { get; init; }
    string Batch { get; init; }
}