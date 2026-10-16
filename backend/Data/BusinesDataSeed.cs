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

        var now = DateTime.UtcNow;
        var mel = new Product { Name = "Mel Silvestre 500g", SKU = "MEL-500-SIL", Desc = "Mel silvestre puro, pote de vidro 500g", Price = 34.90m, IsActive = true, CreatedAt = now, UpdatedAt = now };
        var propolis = new Product { Name = "Própolis Extrato 30ml", SKU = "PROP-30-EXT", Desc = "Extrato de própolis verde, frasco 30ml", Price = 27.50m, IsActive = true, CreatedAt = now, UpdatedAt = now };
        var geleia = new Product { Name = "Geleia Real 10g", SKU = "GELE-10-RAI", Desc = "Geleia real pura, pote 10g", Price = 89.90m, IsActive = true, CreatedAt = now, UpdatedAt = now };
        var cera = new Product { Name = "Cera de Abelha 250g", SKU = "CERA-250-AB", Desc = "Cera de abelha em bloco, 250g", Price = 18.00m, IsActive = true, CreatedAt = now, UpdatedAt = now };
        var melPropolis = new Product { Name = "Mel com Própolis 300g", SKU = "MEL-300-PRO", Desc = "Mel silvestre com própolis, pote 300g", Price = 42.90m, IsActive = true, CreatedAt = now, UpdatedAt = now };

        db.Products.AddRange(mel, propolis, geleia, cera, melPropolis);

        var galpaoSP = new Storage { IdNumber = "SP1", Name = "Galpão São Paulo", AddressStreet = "Rua das Colmeias", AddressNumber = "1250", AddressCity = "São Paulo", IsActive = true, CreatedAt = now, UpdatedAt = now };
        var sitioSRQ = new Storage { IdNumber = "SRQ", Name = "Sítio São Roque", AddressStreet = "Estrada do Apiário, km 12", AddressNumber = "S/N", AddressCity = "São Roque", IsActive = true, CreatedAt = now, UpdatedAt = now };

        db.Storages.AddRange(galpaoSP, sitioSRQ);

        var openingBalances = new (Product Product, Storage Storage, string Batch, int Quantity)[]
        {
            (mel, galpaoSP, "LOTE2026001", 120),
            (mel, galpaoSP, "LOTE2026002", 45),
            (mel, sitioSRQ, "LOTE2026001", 80),
            (propolis, galpaoSP, "LOTE2026001", 200),
            (geleia, sitioSRQ, "LOTE2026001", 30),
            (cera, galpaoSP, "LOTE2026001", 60),
            (melPropolis, sitioSRQ, "LOTE2026001", 15),
        };

        // Cada saldo nasce com o seu movimento de saldo inicial, para a soma dos movimentos fechar
        // com o saldo desde o primeiro boot (as migrations rodam antes do seed, com a base vazia).
        foreach (var (product, storage, batch, quantity) in openingBalances)
        {
            db.StockBalances.Add(new StockBalance { Product = product, Storage = storage, Batch = batch, Balance = quantity });
            db.StockMovements.Add(new StockMovement
            {
                Type = MovementType.OpeningBalance,
                Product = product,
                Storage = storage,
                Batch = batch,
                Quantity = quantity,
                Reason = "Saldo inicial de demonstração",
                CreatedBy = "system"
            });
        }

        await db.SaveChangesAsync();
    }
}