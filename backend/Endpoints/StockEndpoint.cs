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
            // Sem IgnoreQueryFilters, o filtro de soft delete esconderia os lotes de produtos e armazéns
            // apagados, e com eles o único caminho da tela até o histórico desses lotes.
            var responder = await db.StockBalances
                .IgnoreQueryFilters()
                .AsNoTracking()
                .Select(s => new StockResponseDTO(
                    s.ProductId,
                    s.Product.Name,
                    s.Product.Price,
                    s.StorageId,
                    s.Storage.Name,
                    $"{s.Storage.AddressStreet}, {s.Storage.AddressNumber} - {s.Storage.AddressCity}",
                    s.Balance,
                    s.Batch,
                    s.Product.DeletedAt != null,
                    s.Storage.DeletedAt != null
                ))
                .ToListAsync();

            return Results.Ok(responder);
        })
        .WithSummary("Lista todos os saldos de estoque")
        .WithDescription("Retorna os saldos de todos os produtos em todos os armazéns, incluindo lotes de produtos e armazéns excluídos (marcados em ProductDeleted e StorageDeleted) para que o histórico continue acessível. Disponível para Operator e Manager.");
    }
    
}
