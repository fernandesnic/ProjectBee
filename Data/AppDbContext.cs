using ProjectBee.Models;
using Microsoft.EntityFrameworkCore;

namespace ProjectBee.Data;

public class AppDbContext : DbContext
{

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    { 
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Storage> Storages => Set<Storage>();
    public DbSet<StockBalance> StockBalances => Set<StockBalance>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StockBalance>().HasKey(x => new { x.ProductId, x.StorageId, x.Batch});

        modelBuilder.Entity<Product>().Property(x => x.Price).HasPrecision(14, 2);

        base.OnModelCreating(modelBuilder);


    }
}