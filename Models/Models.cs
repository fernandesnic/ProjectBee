namespace ProjectBee.Models;
    public class Product
    {

    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string SKU  { get; set; } = string.Empty;
    public string Desc { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } 


}

public class Storage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int IdNumber { get; set; }
    public int AdressNumber { get; set; }
    public string AddressStreet { get; set; } = string.Empty;
    public string AddressCity { get; set; } = string.Empty;
}

public class StockBalance
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid StorageId { get; set; }
    public Storage Storage { get; set; } = null!;
    public int Balance { get; set; }
    public string Batch {  get; set; } = string.Empty;
}