namespace ProjectBee.Interfaces
{
    public interface IStorageDTO
    {
        string IdNumber { get; init; }
        string AddressNumber { get; init; }
        string AddressStreet { get; init; } 
        string AddressCity { get; init; } 
    }
}
