using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProjectBee.Data;
using ProjectBee.Models;
using ProjectBee.DTO.Storage;

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

            var responseDTO = ToResponse(Storage);

            return Results.Created($"/api/storages/{Storage.Id}", responseDTO);
        })
        .RequireAuthorization(policy => policy.RequireRole(Roles.Manager))
        .WithSummary("Cadastra um armazém")
        .WithDescription("O IdNumber deve ter de 3 a 20 caracteres e ser único, inclusive entre armazéns excluídos. Requer perfil Manager.")
        .Produces<StorageResponseDTO>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

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
                    s.IsActive,
                    s.RowVersion
                    ))
                .ToListAsync();

            return Results.Ok(responder);
        })
        .WithSummary("Lista todos os armazéns")
        .WithDescription("Retorna armazéns ativos e inativos. Disponível para Operator e Manager.")
        .Produces<List<StorageResponseDTO>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized);

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
                    s.IsActive,
                    s.RowVersion
                    ))
                .FirstOrDefaultAsync();

            if (responder == null)
            {
                return Results.NotFound(new { mensagem = $"Armazem não encontrado" });
            }

            return Results.Ok(responder);
        })
        .WithSummary("Busca um armazém por ID")
        .Produces<StorageResponseDTO>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status404NotFound);

        StoragesApi.MapDelete("/{id}", async (AppDbContext db, Guid id) =>
        {
            var Storage = await db.Storages.FindAsync(id);
            if (Storage == null)
            {
                return Results.NotFound(new { mensagem = $"Armazem não encontrado" });
            }

            // Soft delete: a linha continua no banco, então os movimentos antigos seguem apontando para o armazém.
            if (await db.StockBalances.AnyAsync(b => b.StorageId == id && b.Balance != 0))
            {
                return Results.Problem(
                    title: "Exclusão recusada",
                    detail: "O armazém ainda tem saldo. Transfira ou ajuste o estoque para zero antes de excluí-lo.",
                    statusCode: StatusCodes.Status422UnprocessableEntity);
            }

            var now = DateTime.UtcNow;
            Storage.DeletedAt = now;
            Storage.IsActive = false;
            Storage.UpdatedAt = now;
            await db.SaveChangesAsync();

            return Results.NoContent();
        })
        .RequireAuthorization(policy => policy.RequireRole(Roles.Manager))
        .WithSummary("Remove um armazém")
        .WithDescription("Exclusão lógica (soft delete): o armazém some das listagens, mas o histórico de movimentos é preservado. Só é permitida para armazéns sem saldo em nenhum lote; caso contrário retorna 422. Requer perfil Manager.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

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

            // Concorrência otimista, como em produtos: versão velha não sobrescreve a edição de outra pessoa.
            if (!Storage.RowVersion.SequenceEqual(dto.RowVersion!))
                return EditConflict();
            db.Entry(Storage).Property(s => s.RowVersion).OriginalValue = dto.RowVersion!;

            // Armazém inativo não pode ter saldo: o estoque precisa sair por movimento antes.
            if (Storage.IsActive && dto.IsActive == false)
            {
                var balances = await db.StockBalances
                    .Where(b => b.StorageId == id && b.Balance != 0)
                    .GroupBy(b => b.StorageId)
                    .Select(g => new { Lots = g.Count(), Units = g.Sum(b => b.Balance) })
                    .FirstOrDefaultAsync();

                if (balances != null)
                {
                    return Results.Problem(
                        title: "Desativação recusada",
                        detail: $"O armazém ainda tem saldo em {balances.Lots} lote(s), somando {balances.Units} unidade(s). Transfira ou ajuste o estoque para zero antes de desativá-lo.",
                        statusCode: StatusCodes.Status422UnprocessableEntity);
                }
            }

            Storage.Name = dto.Name;
            Storage.AddressCity = dto.AddressCity;
            Storage.AddressStreet = dto.AddressStreet;
            Storage.AddressNumber = dto.AddressNumber;
            Storage.IsActive = dto.IsActive!.Value;
            Storage.UpdatedAt = DateTime.UtcNow;

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return EditConflict();
            }

            return Results.Ok(ToResponse(Storage));
        })
        .RequireAuthorization(policy => policy.RequireRole(Roles.Manager))
        .WithSummary("Atualiza um armazém")
        .WithDescription("O IdNumber não pode ser alterado — ele identifica o armazém. Para trocar de IdNumber, cadastre um novo armazém. O campo IsActive é obrigatório: omiti-lo retorna 400, para evitar desativação acidental. O RowVersion lido antes de editar também é obrigatório: se outra edição gravou no meio, retorna 409 e nada é alterado. Desativar um armazém que ainda tem saldo retorna 422. A resposta traz o armazém com o RowVersion novo.")
        .Produces<StorageResponseDTO>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static StorageResponseDTO ToResponse(Storage s) =>
        new(s.Id, s.Name, s.IdNumber, s.AddressNumber, s.AddressStreet, s.AddressCity, s.IsActive, s.RowVersion);

    private static IResult EditConflict() => Results.Problem(
        title: "Conflito de edição",
        detail: "O armazém foi alterado por outra pessoa enquanto você editava. Recarregue para ver a versão atual e refaça a alteração.",
        statusCode: StatusCodes.Status409Conflict);
}
