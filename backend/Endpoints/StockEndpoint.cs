using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProjectBee.Data;
using ProjectBee.Models;

public static class StockEndpointsExtensions
{
    public static void MapStockEndpoints(this IEndpointRouteBuilder app)
    {
        var stockApi = app.MapGroup("/api/stock")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Operator, Roles.Manager))
            .WithTags("Estoque");

        stockApi.MapPost("/", async (CreateStockDTO dto, IValidator<CreateStockDTO> validator, AppDbContext db) =>
        {
            var validationResult = await validator.ValidateAsync(dto); 

            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

           var checkStock = await db.StockBalances
           .FindAsync(dto.ProductId, dto.StorageId, dto.Batch);

            if (checkStock != null)
            {
                return Results.Conflict(new { mensagem = "Já existe este produto nesse lote e nesse armazem." });
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
        })
        .WithSummary("Registra uma entrada de saldo em estoque")
        .WithDescription("Cria um novo saldo para a combinação de produto, armazém e lote. Retorna conflito se essa combinação já existir. Disponível para Operator e Manager.");

        stockApi.MapGet("/", async (AppDbContext db) =>
        {
            var responder = await db.StockBalances
                .AsNoTracking()
                .Select(s => new StockResponseDTO(
                    s.ProductId,
                    s.Product.Name,
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
        })
        .WithSummary("Atualiza o saldo de estoque")
        .WithDescription("Atualiza o saldo (Balance) de uma combinação existente de produto, armazém e lote. Disponível para Operator e Manager.");

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
        })
        .RequireAuthorization(policy => policy.RequireRole(Roles.Manager))
        .WithSummary("Remove um saldo de estoque")
        .WithDescription("Remove o registro de saldo para a combinação de produto, armazém e lote informada. Requer perfil Manager.");
    }
    
}