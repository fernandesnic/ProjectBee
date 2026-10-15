using Microsoft.EntityFrameworkCore;
using ProjectBee.Data;
using ProjectBee.Models;
using ProjectBee.DTO.Stock;

public static class StockEndpointsExtensions
{
    public static void MapStockEndpoints(this IEndpointRouteBuilder app)
    {
        var stockApi = app.MapGroup("/api/stock")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Operator, Roles.Manager))
            .WithTags("Estoque");

        stockApi.MapGet("/", async (AppDbContext db) =>
        {
            var responder = await db.StockBalances
                .AsNoTracking()
                .Select(s => new StockResponseDTO(
                    s.ProductId,
                    s.Product.Name,
                    s.Product.Price,
                    s.StorageId,
                    s.Storage.Name,
                    $"{s.Storage.AddressStreet}, {s.Storage.AddressNumber} - {s.Storage.AddressCity}",
                    s.Balance,
                    s.Batch
                ))
                .ToListAsync();

            return Results.Ok(responder);
        })
        .WithSummary("Lista todos os saldos de estoque")
        .WithDescription("Retorna os saldos de todos os produtos em todos os armazéns. Disponível para Operator e Manager.");
    }
    
}
