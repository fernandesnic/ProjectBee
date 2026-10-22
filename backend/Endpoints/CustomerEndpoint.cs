using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProjectBee.Data;
using ProjectBee.Models;
using ProjectBee.DTO.Customer;

public static class CustomerEndpointsExtensions
{
    public static void MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        var customersApi = app.MapGroup("/api/customers")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Operator, Roles.Manager))
            .WithTags("Clientes");

        customersApi.MapPost("/", async (CreateCustomerDTO dto, IValidator<CreateCustomerDTO> validator, AppDbContext db) =>
        {
            var validationResult = await validator.ValidateAsync(dto);

            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var customer = new Customer
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

            db.Customers.Add(customer);
            await db.SaveChangesAsync();

            return Results.Created($"/api/customers/{customer.Id}", ToResponse(customer));
        })
        .RequireAuthorization(policy => policy.RequireRole(Roles.Manager))
        .WithSummary("Cadastra um cliente")
        .WithDescription("O documento é o CPF (11 dígitos) ou CNPJ (14 dígitos), só números, e é único inclusive entre clientes excluídos. E-mail e telefone são opcionais. Requer perfil Manager.")
        .Produces<CustomerResponseDTO>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        customersApi.MapGet("/", async (AppDbContext db) =>
        {
            var responder = await db.Customers
                .AsNoTracking()
                .Select(c => new CustomerResponseDTO(c.Id, c.Name, c.Document, c.Email, c.Phone, c.IsActive, c.RowVersion))
                .ToListAsync();

            return Results.Ok(responder);
        })
        .WithSummary("Lista todos os clientes")
        .WithDescription("Retorna clientes ativos e inativos. Disponível para Operator e Manager.")
        .Produces<List<CustomerResponseDTO>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized);

        customersApi.MapGet("/{id}", async (AppDbContext db, Guid id) =>
        {
            var responder = await db.Customers
                .AsNoTracking()
                .Where(c => c.Id == id)
                .Select(c => new CustomerResponseDTO(c.Id, c.Name, c.Document, c.Email, c.Phone, c.IsActive, c.RowVersion))
                .FirstOrDefaultAsync();

            if (responder == null)
            {
                return Results.NotFound(new { mensagem = $"Cliente não encontrado" });
            }

            return Results.Ok(responder);
        })
        .WithSummary("Busca um cliente por ID")
        .Produces<CustomerResponseDTO>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status404NotFound);

        customersApi.MapDelete("/{id}", async (AppDbContext db, Guid id) =>
        {
            var customer = await db.Customers.FindAsync(id);
            if (customer == null)
            {
                return Results.NotFound(new { mensagem = $"Cliente não encontrado" });
            }

            // Pedido ainda não faturado vai gerar nota para este cliente: ele precisa continuar no cadastro.
            var pendingOrders = await db.SalesOrders.CountAsync(o => o.CustomerId == id
                && (o.Status == SalesOrderStatus.AwaitingPayment || o.Status == SalesOrderStatus.Paid));
            if (pendingOrders > 0)
            {
                return Results.Problem(
                    title: "Exclusão recusada",
                    detail: $"O cliente tem {pendingOrders} pedido(s) de venda aguardando pagamento ou faturamento. Fature-os antes de excluí-lo.",
                    statusCode: StatusCodes.Status422UnprocessableEntity);
            }

            var now = DateTime.UtcNow;
            customer.DeletedAt = now;
            customer.IsActive = false;
            customer.UpdatedAt = now;
            await db.SaveChangesAsync();

            return Results.NoContent();
        })
        .RequireAuthorization(policy => policy.RequireRole(Roles.Manager))
        .WithSummary("Remove um cliente")
        .WithDescription("Exclusão lógica (soft delete): o cliente some das listagens, mas o documento continua reservado e os pedidos antigos seguem apontando para ele. Cliente com pedido aguardando pagamento ou faturamento retorna 422. Requer perfil Manager.")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        customersApi.MapPut("/{id}", async (UpdateCustomerDTO dto, IValidator<UpdateCustomerDTO> validator, AppDbContext db, Guid id) =>
        {
            var validationResult = await validator.ValidateAsync(dto);

            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var customer = await db.Customers.FindAsync(id);
            if (customer == null)
            {
                return Results.NotFound(new { mensagem = $"Cliente não encontrado" });
            }

            // Concorrência otimista, como em fornecedores.
            if (!customer.RowVersion.SequenceEqual(dto.RowVersion!))
                return EditConflict();
            db.Entry(customer).Property(c => c.RowVersion).OriginalValue = dto.RowVersion!;

            customer.Name = dto.Name;
            customer.Email = dto.Email ?? string.Empty;
            customer.Phone = dto.Phone ?? string.Empty;
            customer.IsActive = dto.IsActive!.Value;
            customer.UpdatedAt = DateTime.UtcNow;

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                return EditConflict();
            }

            return Results.Ok(ToResponse(customer));
        })
        .RequireAuthorization(policy => policy.RequireRole(Roles.Manager))
        .WithSummary("Atualiza um cliente")
        .WithDescription("O documento não pode ser alterado — ele identifica o cliente. O campo IsActive é obrigatório: omiti-lo retorna 400. O RowVersion lido antes de editar também é obrigatório: se outra edição gravou no meio, retorna 409 e nada é alterado.")
        .Produces<CustomerResponseDTO>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict);
    }

    private static CustomerResponseDTO ToResponse(Customer c) =>
        new(c.Id, c.Name, c.Document, c.Email, c.Phone, c.IsActive, c.RowVersion);

    private static IResult EditConflict() => Results.Problem(
        title: "Conflito de edição",
        detail: "O cliente foi alterado por outra pessoa enquanto você editava. Recarregue para ver a versão atual e refaça a alteração.",
        statusCode: StatusCodes.Status409Conflict);
}
