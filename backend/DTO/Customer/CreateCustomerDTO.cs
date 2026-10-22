using ProjectBee.Interfaces;

namespace ProjectBee.DTO.Customer;

public record CreateCustomerDTO(string Name, string Document, string Email, string Phone) : ICustomerDTO;
