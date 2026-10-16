using Microsoft.AspNetCore.Identity;

namespace ProjectBee.Models;

public class AppUser : IdentityUser
{
    public string Nome { get; set; } = string.Empty;
}

    public class Product
    {

    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string SKU  { get; set; } = string.Empty;
    public string Desc { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }


}

public class Storage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name {get; set;} = string.Empty;
    public string IdNumber { get; set; } = string.Empty;
    public string AddressNumber { get; set; } = string.Empty;
    public string AddressStreet { get; set; } = string.Empty;
    public string AddressCity { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}

public class StockBalance
{
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid StorageId { get; set; }
    public Storage Storage { get; set; } = null!;
    public int Balance { get; set; }
    public string Batch {  get; set; } = string.Empty;
    public byte[] RowVersion { get; set; } = null!; // controle de concorrência otimista
}

public enum MovementType
{
    Entry = 1,        // Entrada
    Exit = 2,         // Saída
    TransferOut = 3,  // Transferência - sai da origem (RE4)
    TransferIn = 4,   // Transferência - entra no destino (DE4)
    Adjustment = 5,     // Ajuste de inventário
    OpeningBalance = 6  // Saldo inicial (implantação do livro-razão)
}

public class StockMovement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public MovementType Type { get; set; }

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid StorageId { get; set; }
    public Storage Storage { get; set; } = null!;
    public string Batch { get; set; } = string.Empty;

    public int Quantity { get; set; }          // com sinal: + soma, - subtrai
    public string? Reason { get; set; }        // obrigatório só no Ajuste
    public Guid? TransferId { get; set; }      // liga o par da transferência

    public string CreatedBy { get; set; } = string.Empty; // e-mail do usuário
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}