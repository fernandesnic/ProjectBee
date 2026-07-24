using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProjectBee.Data;
using ProjectBee.Models;

public static class StorageEndpointsExtensions
{
    public static void MapStorageEndpoints(this IEndpointRouteBuilder app)
    {
        var StoragesApi = app.MapGroup("/api/storages");

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
                AddressNumber = dto.AddressNumber,
                AddressCity = dto.AddressCity,
                AddressStreet = dto.AddressStreet,
                Id = Guid.NewGuid(),
                IsActive = true,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            db.Storages.Add(Storage);
            await db.SaveChangesAsync();

            // Ordem corrigida aqui!
            var responseDTO = new StorageResponseDTO(Storage.Id, Storage.IdNumber, Storage.AddressNumber, Storage.AddressStreet, Storage.AddressCity);

            return Results.Created($"/api/storages/{Storage.Id}", responseDTO);
        });

        StoragesApi.MapGet("/", async (AppDbContext db) =>
        {
            var Storages = await db.Storages.ToListAsync();
            // Ordem corrigida aqui!
            var responder = Storages.Select(s => new StorageResponseDTO(
                s.Id,
                s.IdNumber,
                s.AddressNumber,
                s.AddressStreet,
                s.AddressCity
                )).ToList();

            return Results.Ok(responder);
        });

        StoragesApi.MapGet("/{id}", async (AppDbContext db, Guid id) =>
        {
            var Storage = await db.Storages.FindAsync(id);
            if (Storage == null)
            {
                return Results.NotFound(new { mensagem = $"Armazem não encontrado" });
            }

            // Ordem corrigida aqui!
            var responder = new StorageResponseDTO(
                Storage.Id,
                Storage.IdNumber,
                Storage.AddressNumber,
                Storage.AddressStreet,
                Storage.AddressCity
                );

            return Results.Ok(responder);
        });

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
        });

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

            Storage.IdNumber = dto.IdNumber;
            Storage.AddressCity = dto.AddressCity;
            Storage.AddressStreet = dto.AddressStreet;
            Storage.AddressNumber = dto.AddressNumber;
            Storage.IsActive = dto.IsActive;
            Storage.UpdatedAt = DateTime.Now;

            await db.SaveChangesAsync();

            return Results.Ok(new { mensagem = $"Armazem atualizado com sucesso" });
        });
    }
}