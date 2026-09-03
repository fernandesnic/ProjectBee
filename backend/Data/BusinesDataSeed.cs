using Microsoft.EntityFrameworkCore;
using ProjectBee.Models;

namespace ProjectBee.Data;

public static class BusinessDataSeedExtensions
{
    public static async Task SeedBusinessDataAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (await db.Products.AnyAsync())
        {
            return; // já tem dado, não duplica em cada restart
        }

        var mel = new Product { Name = "Mel Silvestre 500g", SKU = "MEL-500-SIL", Desc = "Mel silvestre puro, pote de vidro 500g", Price = 34.90m, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var propolis = new Product { Name = "Própolis Extrato 30ml", SKU = "PROP-30-EXT", Desc = "Extrato de própolis verde, frasco 30ml", Price = 27.50m, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var geleia = new Product { Name = "Geleia Real 10g", SKU = "GELE-10-RAI", Desc = "Geleia real pura, pote 10g", Price = 89.90m, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var cera = new Product { Name = "Cera de Abelha 250g", SKU = "CERA-250-AB", Desc = "Cera de abelha em bloco, 250g", Price = 18.00m, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var melPropolis = new Product { Name = "Mel com Própolis 300g", SKU = "MEL-300-PRO", Desc = "Mel silvestre com própolis, pote 300g", Price = 42.90m, IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        db.Products.AddRange(mel, propolis, geleia, cera, melPropolis);

        var galpaoSP = new Storage { IdNumber = "SP1", Name = "Galpão São Paulo", AddressStreet = "Rua das Colmeias", AddressNumber = "1250", AddressCity = "São Paulo", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        var sitioSRQ = new Storage { IdNumber = "SRQ", Name = "Sítio São Roque", AddressStreet = "Estrada do Apiário, km 12", AddressNumber = "S/N", AddressCity = "São Roque", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };

        db.Storages.AddRange(galpaoSP, sitioSRQ);

        db.StockBalances.AddRange(
            new StockBalance { Product = mel, Storage = galpaoSP, Batch = "LOTE2026001", Balance = 120 },
            new StockBalance { Product = mel, Storage = galpaoSP, Batch = "LOTE2026002", Balance = 45 },
            new StockBalance { Product = mel, Storage = sitioSRQ, Batch = "LOTE2026001", Balance = 80 },
            new StockBalance { Product = propolis, Storage = galpaoSP, Batch = "LOTE2026001", Balance = 200 },
            new StockBalance { Product = geleia, Storage = sitioSRQ, Batch = "LOTE2026001", Balance = 30 },
            new StockBalance { Product = cera, Storage = galpaoSP, Batch = "LOTE2026001", Balance = 60 },
            new StockBalance { Product = melPropolis, Storage = sitioSRQ, Batch = "LOTE2026001", Balance = 15 }
        );

        await db.SaveChangesAsync();
    }
}