using ProjectBee.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace ProjectBee.Data;

public class AppDbContext : IdentityDbContext<AppUser>
{

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    { 
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Storage> Storages => Set<Storage>();
    public DbSet<StockBalance> StockBalances => Set<StockBalance>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();
    public DbSet<PurchaseReceipt> PurchaseReceipts => Set<PurchaseReceipt>();
    public DbSet<SalesOrder> SalesOrders => Set<SalesOrder>();
    public DbSet<Boleto> Boletos => Set<Boleto>();
    public DbSet<SalesInvoice> SalesInvoices => Set<SalesInvoice>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<StockBalance>().HasKey(x => new { x.ProductId, x.StorageId, x.Batch });
    modelBuilder.Entity<StockBalance>().Property(x => x.RowVersion).IsRowVersion();

    modelBuilder.Entity<Product>().Property(x => x.Price).HasPrecision(14, 2);
    modelBuilder.Entity<Product>().Property(x => x.SKU).HasMaxLength(15);
    modelBuilder.Entity<Product>().HasIndex(x => x.SKU).IsUnique();

    modelBuilder.Entity<Product>().Property(x => x.RowVersion).IsRowVersion();
    modelBuilder.Entity<Storage>().Property(x => x.RowVersion).IsRowVersion();

    modelBuilder.Entity<Storage>().Property(x => x.IdNumber).HasMaxLength(20);
    modelBuilder.Entity<Storage>().HasIndex(x => x.IdNumber).IsUnique();

    modelBuilder.Entity<Supplier>(e =>
    {
        e.Property(x => x.Name).HasMaxLength(150);
        e.Property(x => x.Document).HasMaxLength(14);
        e.HasIndex(x => x.Document).IsUnique();
        e.Property(x => x.Email).HasMaxLength(256);
        e.Property(x => x.Phone).HasMaxLength(20);
        e.Property(x => x.RowVersion).IsRowVersion();
        e.HasQueryFilter(x => x.DeletedAt == null);
    });

    modelBuilder.Entity<Customer>(e =>
    {
        e.Property(x => x.Name).HasMaxLength(150);
        e.Property(x => x.Document).HasMaxLength(14);
        e.HasIndex(x => x.Document).IsUnique();
        e.Property(x => x.Email).HasMaxLength(256);
        e.Property(x => x.Phone).HasMaxLength(20);
        e.Property(x => x.RowVersion).IsRowVersion();
        e.HasQueryFilter(x => x.DeletedAt == null);
    });

    modelBuilder.Entity<Product>().HasQueryFilter(p => p.DeletedAt == null);
    modelBuilder.Entity<Storage>().HasQueryFilter(s => s.DeletedAt == null);

    modelBuilder.Entity<StockMovement>(e =>
    {
        e.Property(x => x.Batch).HasMaxLength(30);
        e.Property(x => x.Reason).HasMaxLength(250);
        e.Property(x => x.CreatedBy).HasMaxLength(256);

        e.HasIndex(x => new { x.ProductId, x.StorageId, x.Batch });
        e.HasIndex(x => x.TransferId);

        e.HasOne(x => x.Product).WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.Storage).WithMany()
        .HasForeignKey(x => x.StorageId)
        .OnDelete(DeleteBehavior.Restrict);

        e.HasOne<PurchaseReceipt>().WithMany()
            .HasForeignKey(x => x.PurchaseReceiptId)
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne<SalesInvoice>().WithMany()
            .HasForeignKey(x => x.SalesInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);
        });

    modelBuilder.Entity<PurchaseOrder>(e =>
    {
        // Número legível do pedido (PC-000123), gerado pelo banco para não repetir entre requisições simultâneas.
        e.Property(x => x.Number).UseIdentityColumn();
        e.Property(x => x.Number).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
        e.HasIndex(x => x.Number).IsUnique();
        e.HasIndex(x => x.Status);

        e.Property(x => x.Notes).HasMaxLength(500);
        e.Property(x => x.CreatedBy).HasMaxLength(256);
        e.Property(x => x.CancelledBy).HasMaxLength(256);
        e.Property(x => x.CancelReason).HasMaxLength(250);
        e.Property(x => x.RowVersion).IsRowVersion();

        e.HasOne(x => x.Supplier).WithMany()
            .HasForeignKey(x => x.SupplierId)
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.Storage).WithMany()
            .HasForeignKey(x => x.StorageId)
            .OnDelete(DeleteBehavior.Restrict);

        e.HasMany(x => x.Items).WithOne()
            .HasForeignKey(x => x.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        e.HasMany(x => x.Receipts).WithOne(x => x.PurchaseOrder)
            .HasForeignKey(x => x.PurchaseOrderId)
            .OnDelete(DeleteBehavior.Restrict);
    });

    modelBuilder.Entity<PurchaseOrderItem>(e =>
    {
        e.Property(x => x.UnitPrice).HasPrecision(14, 2);
        // Um produto aparece uma vez por pedido: o recebimento aponta para a linha sem ambiguidade.
        e.HasIndex(x => new { x.PurchaseOrderId, x.ProductId }).IsUnique();

        e.HasOne(x => x.Product).WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    });

    modelBuilder.Entity<PurchaseReceipt>(e =>
    {
        e.Property(x => x.InvoiceNumber).HasMaxLength(20);
        e.HasIndex(x => x.InvoiceNumber);
        e.Property(x => x.PriceDivergenceReason).HasMaxLength(250);
        e.Property(x => x.CreatedBy).HasMaxLength(256);

        e.HasMany(x => x.Items).WithOne()
            .HasForeignKey(x => x.PurchaseReceiptId)
            .OnDelete(DeleteBehavior.Cascade);
    });

    modelBuilder.Entity<PurchaseReceiptItem>(e =>
    {
        e.Property(x => x.Batch).HasMaxLength(30);
        e.Property(x => x.UnitPrice).HasPrecision(14, 2);

        e.HasOne(x => x.PurchaseOrderItem).WithMany()
            .HasForeignKey(x => x.PurchaseOrderItemId)
            .OnDelete(DeleteBehavior.Restrict);
    });

    modelBuilder.Entity<SalesOrder>(e =>
    {
        // Número legível do pedido (PV-000123), gerado pelo banco como o do pedido de compra.
        e.Property(x => x.Number).UseIdentityColumn();
        e.Property(x => x.Number).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
        e.HasIndex(x => x.Number).IsUnique();
        e.HasIndex(x => x.Status);

        e.Property(x => x.Notes).HasMaxLength(500);
        e.Property(x => x.CreatedBy).HasMaxLength(256);
        e.Property(x => x.RowVersion).IsRowVersion();

        e.HasOne(x => x.Customer).WithMany()
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.Storage).WithMany()
            .HasForeignKey(x => x.StorageId)
            .OnDelete(DeleteBehavior.Restrict);

        e.HasMany(x => x.Items).WithOne()
            .HasForeignKey(x => x.SalesOrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // Um boleto e uma nota por pedido: o índice único impede faturar o mesmo pedido duas vezes.
        e.HasOne(x => x.Boleto).WithOne()
            .HasForeignKey<Boleto>(x => x.SalesOrderId)
            .OnDelete(DeleteBehavior.Restrict);

        e.HasOne(x => x.Invoice).WithOne(x => x.SalesOrder)
            .HasForeignKey<SalesInvoice>(x => x.SalesOrderId)
            .OnDelete(DeleteBehavior.Restrict);
    });

    modelBuilder.Entity<SalesOrderItem>(e =>
    {
        e.Property(x => x.UnitPrice).HasPrecision(14, 2);
        e.HasIndex(x => new { x.SalesOrderId, x.ProductId }).IsUnique();

        e.HasOne(x => x.Product).WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    });

    modelBuilder.Entity<Boleto>(e =>
    {
        e.Property(x => x.OurNumber).HasMaxLength(17);
        e.HasIndex(x => x.OurNumber).IsUnique();
        e.Property(x => x.DigitableLine).HasMaxLength(47);
        e.Property(x => x.Amount).HasPrecision(14, 2);
        e.Property(x => x.PaidBy).HasMaxLength(256);
    });

    modelBuilder.Entity<SalesInvoice>(e =>
    {
        // Número da nota (NF-000123): sequencial e sem repetição, gerado pelo banco.
        e.Property(x => x.Number).UseIdentityColumn();
        e.Property(x => x.Number).Metadata.SetAfterSaveBehavior(PropertySaveBehavior.Ignore);
        e.HasIndex(x => x.Number).IsUnique();
        e.Property(x => x.CreatedBy).HasMaxLength(256);

        e.HasMany(x => x.Items).WithOne()
            .HasForeignKey(x => x.SalesInvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
    });

    modelBuilder.Entity<SalesInvoiceItem>(e =>
    {
        e.Property(x => x.Batch).HasMaxLength(30);
        e.Property(x => x.UnitPrice).HasPrecision(14, 2);

        e.HasOne(x => x.Product).WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    });
    }
}