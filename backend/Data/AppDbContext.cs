using ProjectBee.Models;
using Microsoft.EntityFrameworkCore;
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
        });
    }
}