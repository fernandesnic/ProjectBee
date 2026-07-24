using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProjectBee.Data;
using ProjectBee.Models;

public static class StockEndpointsExtensions
{
    public static void MapStockEndpoints(this IEndpointRouteBuilder app)
    {
        var stockApi = app.MapGroup("/api/stock");

        stockApi.MapPost("/", async (CreateStockDTO dto, IValidator<CreateStockDTO> validator, AppDbContext db) =>
        {
            var validationResult = await validator.ValidateAsync(dto); 

            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var stock = new StockBalance
            {
                ProductId = dto.ProductId,
                StorageId = dto.StorageId,
                Balance = dto.Balance,
                Batch = dto.Batch
            };

            db.StockBalances.Add(stock);
            await db.SaveChangesAsync();

            return Results.Ok(new { mensagem = "Entrada de saldo registrada com sucesso!" });
        });

        stockApi.MapGet("/", async (AppDbContext db) =>
        {
            var stocks = await db.StockBalances
                .Include(s => s.Product)
                .Include(s => s.Storage)
                .ToListAsync();

            var responder = stocks.Select(s => new StockResponseDTO(
                s.ProductId,
                s.Product.Name, 
                s.StorageId,
                s.Storage.AddressStreet, 
                s.Balance,
                s.Batch
            )).ToList();

            return Results.Ok(responder);
        });

        // 3. ENDPOINT DE ATUALIZAÇÃO (PUT)
        stockApi.MapPut("/{productId}/{storageId}/{batch}", async (
            Guid productId, 
            Guid storageId, 
            string batch, 
            UpdateStockDTO dto, 
            IValidator<UpdateStockDTO> validator, 
            AppDbContext db) =>
        {
            // Valida o DTO que veio no corpo da requisição
            var validationResult = await validator.ValidateAsync(dto);

            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var stock = await db.StockBalances
               .FindAsync(productId, storageId, batch);

            if (stock == null)
            {
                return Results.NotFound(new { mensagem = "Saldo não encontrado para este produto neste armazém." });
            }

            stock.Balance = dto.Balance;

            await db.SaveChangesAsync();

            return Results.Ok(new { mensagem = "Saldo atualizado com sucesso!" });
        });

        stockApi.MapDelete("/{productId}/{storageId}/{batch}", async (AppDbContext db, Guid productId, Guid storageId, String batch) =>
        {
            var stock = await db.StockBalances
                .FindAsync(productId, storageId, batch);


            if (stock == null)
            {
                return Results.NotFound(new { mensagem = "Saldo não encontrado para este produto neste armazém." });
            }
            
            db.StockBalances.Remove(stock);
            await db.SaveChangesAsync();

            return Results.NoContent();
        });
    }
    
}