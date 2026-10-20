using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProjectBee.Data;
using ProjectBee.Models;
using ProjectBee.DTO.Supplier;

public static class SupplierEndpointsExtensions
{
    public static void MapSupplierEndpoints(this IEndpointRouteBuilder app)
    {
        var suppliersApi = app.MapGroup("/api/suppliers")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Operator, Roles.Manager))
            .WithTags("Fornecedores");

        suppliersApi.MapPost("/", async (CreateSupplierDTO dto, IValidator<CreateSupplierDTO> validator, AppDbContext db) =>
        {
            var validationResult = await validator.ValidateAsync(dto);

            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var supplier = new Supplier
            {
                Name = dto.Name,
                Document = dto.Document,
                Email = dto.Email ?? string.Empty,
                Phone = dto.Phone ?? string.Empty,
                Id = Guid.NewGuid(),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            db.Suppliers.Add(supplier);
            await db.SaveChangesAsync();

            return Results.Created($"/api/suppliers/{supplier.Id}", ToResponse(supplier));
        })
        .RequireAuthorization(policy => policy.RequireRole(Roles.Manager))
        .WithSummary("Cadastra um fornecedor")
        .WithDescription("O documento é o CPF (11 dígitos) ou CNPJ (14 dígitos), só números, e é único inclusive entre fornecedores excluídos. E-mail e telefone são opcionais. Requer perfil Manager.")
        .Produces<SupplierResponseDTO>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        suppliersApi.MapGet("/", async (AppDbContext db) =>
        {
            var responder = await db.Suppliers
                .AsNoTracking()
                .Select(s => new SupplierResponseDTO(s.Id, s.Name, s.Document, s.Email, s.Phone, s.IsActive, s.RowVersion))
                .ToListAsync();

            return Results.Ok(responder);
        })
        .WithSummary("Lista todos os fornecedores")
        .WithDescription("Retorna fornecedores ativos e inativos. Disponível para Operator e Manager.")
        .Produces<List<SupplierResponseDTO>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized);

        suppliersApi.MapGet("/{id}", async (AppDbContext db, Guid id) =>
        {
            var responder = await db.Suppliers
                .AsNoTracking()
                .Where(s => s.Id == id)
                .Select(s => new SupplierResponseDTO(s.Id, s.Name, s.Document, s.Email, s.Phone, s.IsActive, s.RowVersion))
                .FirstOrDefaultAsync();

            if (responder == null)
            {
                return Results.NotFound(new { mensagem = $"Fornecedor não encontrado" });
            }

            return Results.Ok(responder);
        })
        .WithSummary("Busca um fornecedor por ID")
        .Produces<SupplierResponseDTO>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status404NotFound);

        suppliersApi.MapDelete("/{id}", async (AppDbContext db, Guid id) =>
        {
            var supplier = await db.Suppliers.FindAsync(id);
            if (supplier == null)
            {
                return Results.NotFound(new { mensagem = $"Fornecedor não encontrado" });
            }

            // Pedido em aberto ainda vai gerar nota e conta a pagar: o fornecedor precisa continuar no cadastro.
            var pendingOrders = await db.PurchaseOrders.CountAsync(o => o.SupplierId == id
                && (o.Status == PurchaseOrderStatus.Open || o.Status == PurchaseOrderStatus.Partial));
            if (pendingOrders > 0)
            {
                return Results.Problem(
                    title: "Exclusão recusada",
                    detail: $"O fornecedor tem {pendingOrders} pedido(s) de compra aberto(s) ou parcial(is). Receba ou cancele o pendente antes de excluí-lo.",
                    statusCode: StatusCodes.Status422UnprocessableEntity);
            }

            var now = DateTime.UtcNow;
            supplier.DeletedAt = now;
            supplier.IsActive = false;
            supplier.UpdatedAt = now;
            await db.SaveChangesAsync();

            return Results.NoContent();
        })
        .RequireAuthorization(policy => policy.RequireRole(Roles.Manager))
        .WithSummary("Remove um fornecedor")
        .WithDescription("Exclusão lógica (soft delete): o fornecedor some das listagens, mas o documento continua reservado e os pedidos antigos seguem apontando para ele. Fornecedor com pedido aberto ou parcial retorna 422. Requer perfil Manager.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        suppliersApi.MapPut("/{id}", async (UpdateSupplierDTO dto, IValidator<UpdateSupplierDTO> validator, AppDbContext db, Guid id) =>
        {
            var validationResult = await validator.ValidateAsync(dto);

            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var supplier = await db.Suppliers.FindAsync(id);
            if (supplier == null)
            {
                return Results.NotFound(new { mensagem = $"Fornecedor não encontrado" });
            }

            // Concorrência otimista, como em produtos e armazéns.
            if (!supplier.RowVersion.SequenceEqual(dto.RowVersion!))
                return EditConflict();
            db.Entry(supplier).Property(s => s.RowVersion).OriginalValue = dto.RowVersion!;

            supplier.Name = dto.Name;
            supplier.Email = dto.Email ?? string.Empty;
            supplier.Phone = dto.Phone ?? string.Empty;
            supplier.IsActive = dto.IsActive!.Value;
            supplier.UpdatedAt = DateTime.UtcNow;

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return EditConflict();
            }

            return Results.Ok(ToResponse(supplier));
        })
        .RequireAuthorization(policy => policy.RequireRole(Roles.Manager))
        .WithSummary("Atualiza um fornecedor")
        .WithDescription("O documento não pode ser alterado — ele identifica o fornecedor. O campo IsActive é obrigatório: omiti-lo retorna 400. O RowVersion lido antes de editar também é obrigatório: se outra edição gravou no meio, retorna 409 e nada é alterado. Fornecedor inativo não recebe pedidos de compra novos.")
        .Produces<SupplierResponseDTO>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static SupplierResponseDTO ToResponse(Supplier s) =>
        new(s.Id, s.Name, s.Document, s.Email, s.Phone, s.IsActive, s.RowVersion);

    private static IResult EditConflict() => Results.Problem(
        title: "Conflito de edição",
        detail: "O fornecedor foi alterado por outra pessoa enquanto você editava. Recarregue para ver a versão atual e refaça a alteração.",
        statusCode: StatusCodes.Status409Conflict);
}
