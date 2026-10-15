using ProjectBee.Interfaces;

namespace ProjectBee.DTO.Stock;

public record CreateStockDTO(Guid ProductId, Guid StorageId, int Balance, string Batch) : IStockDTO;
