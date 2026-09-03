using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProjectBee.Data;
using ProjectBee.Models;

public static class StorageEndpointsExtensions
{
    public static void MapStorageEndpoints(this IEndpointRouteBuilder app)
    {
        var StoragesApi = app.MapGroup("/api/storages")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Operator, Roles.Manager))
            .WithTags("Armazéns");

        StoragesApi.MapPost("/", async (CreateStorageDTO dto, IValidator<CreateStorageDTO> validator, AppDbContext db) =>
        {
            var validationResult = await validator.ValidateAsync(dto);

            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var Storage = new Storage
            {
                IdNumber = dto.IdNumber,
                Name = dto.Name,
                AddressNumber = dto.AddressNumber,
                AddressCity = dto.AddressCity,
                AddressStreet = dto.AddressStreet,
                Id = Guid.NewGuid(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.Storages.Add(Storage);
            await db.SaveChangesAsync();

            var responseDTO = new StorageResponseDTO(Storage.Id, Storage.Name, Storage.IdNumber, Storage.AddressNumber, Storage.AddressStreet, Storage.AddressCity, Storage.IsActive);

            return Results.Created($"/api/storages/{Storage.Id}", responseDTO);
        })
        .RequireAuthorization(policy => policy.RequireRole(Roles.Manager))
        .WithSummary("Cadastra um armazém")
        .WithDescription("O IdNumber deve ter no mínimo 3 caracteres. Requer perfil Manager.");

        StoragesApi.MapGet("/", async (AppDbContext db) =>
        {
            var responder = await db.Storages
                .AsNoTracking()
                .Select(s => new StorageResponseDTO(
                    s.Id,
                    s.Name,
                    s.IdNumber,
                    s.AddressNumber,
                    s.AddressStreet,
                    s.AddressCity,
                    s.IsActive
                    ))
                .ToListAsync();

            return Results.Ok(responder);
        })
        .WithSummary("Lista todos os armazéns")
        .WithDescription("Retorna armazéns ativos e inativos. Disponível para Operator e Manager.");

        StoragesApi.MapGet("/{id}", async (AppDbContext db, Guid id) =>
        {
            var responder = await db.Storages
                .AsNoTracking()
                .Where(s => s.Id == id)
                .Select(s => new StorageResponseDTO(
                    s.Id,
                    s.Name,
                    s.IdNumber,
                    s.AddressNumber,
                    s.AddressStreet,
                    s.AddressCity,
                    s.IsActive
                    ))
                .FirstOrDefaultAsync();

            if (responder == null)
            {
                return Results.NotFound(new { mensagem = $"Armazem não encontrado" });
            }

            return Results.Ok(responder);
        })
        .WithSummary("Busca um armazém por ID");

        StoragesApi.MapDelete("/{id}", async (AppDbContext db, Guid id) =>
        {
            var Storage = await db.Storages.FindAsync(id);
            if (Storage == null)
            {
                return Results.NotFound(new { mensagem = $"Armazem não encontrado" });
            }

            db.Storages.Remove(Storage);
            await db.SaveChangesAsync();

            return Results.NoContent();
        })
        .RequireAuthorization(policy => policy.RequireRole(Roles.Manager))
        .WithSummary("Remove um armazém")
        .WithDescription("A exclusão é em cascata: os saldos de estoque vinculados a este armazém também são removidos.");

        StoragesApi.MapPut("/{id}", async (UpdateStorageDTO dto, IValidator<UpdateStorageDTO> validator, AppDbContext db, Guid id) =>
        {
            var validationResult = await validator.ValidateAsync(dto);

            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var Storage = await db.Storages.FindAsync(id);
            if (Storage == null)
            {
                return Results.NotFound(new { mensagem = $"Armazem não encontrado" });
            }

            Storage.Name = dto.Name;
            Storage.AddressCity = dto.AddressCity;
            Storage.AddressStreet = dto.AddressStreet;
            Storage.AddressNumber = dto.AddressNumber;
            Storage.IsActive = dto.IsActive!.Value;
            Storage.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync();

            return Results.Ok(new { mensagem = $"Armazem atualizado com sucesso" });
        })
        .RequireAuthorization(policy => policy.RequireRole(Roles.Manager))
        .WithSummary("Atualiza um armazém")
        .WithDescription("O IdNumber não pode ser alterado — ele identifica o armazém. Para trocar de IdNumber, cadastre um novo armazém. O campo IsActive é obrigatório: omiti-lo retorna 400, para evitar desativação acidental.");
    }
}