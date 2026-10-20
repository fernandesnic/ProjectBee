using ProjectBee.Interfaces;

namespace ProjectBee.DTO.Supplier;

public record CreateSupplierDTO(string Name, string Document, string Email, string Phone) : ISupplierDTO;
