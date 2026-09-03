namespace ProjectBee.Interfaces
{
    public interface IStorageDTO
    {
        string Name {get; init; }
        string AddressNumber { get; init; }
        string AddressStreet { get; init; } 
        string AddressCity { get; init; } 
    }
}
