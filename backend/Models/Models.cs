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
    public byte[] RowVersion { get; set; } = null!; // controle de concorrência otimista na edição


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
    public byte[] RowVersion { get; set; } = null!; // controle de concorrência otimista na edição
}

public class Supplier
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Document { get; set; } = string.Empty; // CNPJ ou CPF, só dígitos
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public byte[] RowVersion { get; set; } = null!; // controle de concorrência otimista na edição
}

public class Customer
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Document { get; set; } = string.Empty; // CNPJ ou CPF, só dígitos
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public byte[] RowVersion { get; set; } = null!; // controle de concorrência otimista na edição
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
    OpeningBalance = 6, // Saldo inicial (implantação do livro-razão)
    PurchaseReceipt = 7 // Entrada pelo recebimento de um pedido de compra
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
    public Guid? PurchaseReceiptId { get; set; } // recebimento (nota) que originou a entrada

    public string CreatedBy { get; set; } = string.Empty; // e-mail do usuário
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum PurchaseOrderStatus
{
    Open = 1,      // Aberto: aprovado, aguardando recebimento
    Partial = 2,   // Parcial: parte recebida, saldo pendente
    Received = 3,  // Recebido: todos os itens recebidos
    Cancelled = 4  // Cancelado: o pendente foi cancelado (o que já chegou continua no estoque)
}

public class PurchaseOrder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int Number { get; set; }            // número sequencial, gerado pelo banco
    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;
    public Guid StorageId { get; set; }        // armazém (filial) que recebe a mercadoria
    public Storage Storage { get; set; } = null!;
    public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Open;
    public string? Notes { get; set; }

    public List<PurchaseOrderItem> Items { get; set; } = [];
    public List<PurchaseReceipt> Receipts { get; set; } = [];

    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? CancelledBy { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancelReason { get; set; }
    public byte[] RowVersion { get; set; } = null!; // dois recebimentos ao mesmo tempo: o segundo recebe 409
}

public class PurchaseOrderItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PurchaseOrderId { get; set; }
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }          // pedida
    public decimal UnitPrice { get; set; }     // negociado no pedido
    public int ReceivedQuantity { get; set; }  // soma dos recebimentos
}

// Um recebimento corresponde a uma nota fiscal do fornecedor. É dele que sairão as contas a pagar.
public class PurchaseReceipt
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; } = null!;
    public string InvoiceNumber { get; set; } = string.Empty;
    public string? PriceDivergenceReason { get; set; } // obrigatório quando o preço da nota difere do pedido
    public List<PurchaseReceiptItem> Items { get; set; } = [];
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class PurchaseReceiptItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PurchaseReceiptId { get; set; }
    public Guid PurchaseOrderItemId { get; set; }
    public PurchaseOrderItem PurchaseOrderItem { get; set; } = null!;
    public string Batch { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }     // preço da nota, que pode diferir do pedido
}
