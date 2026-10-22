using System.Security.Claims;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProjectBee.Data;
using ProjectBee.DTO.Sales;
using ProjectBee.Models;
using ProjectBee.Services;

public static class SalesOrderEndpointsExtensions
{
    public static void MapSalesOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var salesApi = app.MapGroup("/api/sales-orders")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Operator, Roles.Manager))
            .WithTags("Faturamento");

        salesApi.MapGet("/", async (AppDbContext db, SalesOrderStatus? status, Guid? customerId) =>
        {
            // IgnoreQueryFilters: o pedido continua listado mesmo que o cliente ou o armazém tenham sido excluídos.
            var query = db.SalesOrders.IgnoreQueryFilters().AsNoTracking();

            if (status.HasValue)
                query = query.Where(o => o.Status == status.Value);

            if (customerId.HasValue)
                query = query.Where(o => o.CustomerId == customerId.Value);

            var orders = await query
                .OrderByDescending(o => o.Number)
                .Take(200)
                .Select(o => new SalesOrderSummaryDTO(
                    o.Id,
                    o.Number,
                    o.Status.ToString(),
                    o.PaymentMethod.ToString(),
                    o.CustomerId,
                    o.Customer.Name,
                    o.StorageId,
                    o.Storage.Name,
                    o.Items.Sum(i => i.Quantity * i.UnitPrice),
                    o.Items.Sum(i => i.Quantity),
                    o.CreatedAt))
                .ToListAsync();

            return Results.Ok(orders);
        })
        .WithSummary("Lista os pedidos de venda")
        .WithDescription("Retorna os 200 pedidos mais recentes, do número maior para o menor, filtráveis por status (AwaitingPayment, Paid, Invoiced) e cliente. Disponível para Operator e Manager.")
        .Produces<List<SalesOrderSummaryDTO>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized);

        salesApi.MapGet("/{id}", async (AppDbContext db, Guid id) =>
        {
            var order = await LoadDetailAsync(db, id);
            return order == null ? NotFound() : Results.Ok(order);
        })
        .WithSummary("Busca um pedido de venda com itens, boleto e nota")
        .WithDescription("O boleto traz a linha digitável (47 dígitos, sem pontuação), o valor, o vencimento e o status. A nota aparece depois do faturamento. O RowVersion volta no pagamento e no faturamento.")
        .Produces<SalesOrderDetailDTO>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status404NotFound);

        salesApi.MapPost("/", async (
            CreateSalesOrderDTO dto,
            IValidator<CreateSalesOrderDTO> validator,
            SalesService service,
            AppDbContext db,
            ClaimsPrincipal user) =>
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var result = await service.CreateOrderAsync(dto, GetUserEmail(user));
            if (!result.Success)
                return ToHttpResult(result);

            return Results.Created($"/api/sales-orders/{result.Id}", await LoadDetailAsync(db, result.Id!.Value));
        })
        .RequireAuthorization(policy => policy.RequireRole(Roles.Manager))
        .WithSummary("Cria um pedido de venda e emite o boleto")
        .WithDescription("O pedido nasce Aguardando pagamento, com o boleto (simulado) já emitido para 3 dias. Cliente, armazém e produtos precisam existir e estar ativos; o preço de cada item é o do produto. Só a condição Boleto é aceita por enquanto. Retorna 422 quando o armazém não tem saldo disponível (saldo físico menos o já vendido em pedidos não faturados). Requer perfil Manager.")
        .Produces<SalesOrderDetailDTO>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        salesApi.MapPost("/{id}/boleto/payment", async (
            Guid id,
            PayBoletoDTO dto,
            IValidator<PayBoletoDTO> validator,
            SalesService service,
            AppDbContext db,
            ClaimsPrincipal user) =>
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var result = await service.PayBoletoAsync(id, dto, GetUserEmail(user));
            if (!result.Success)
                return ToHttpResult(result);

            return Results.Ok(await LoadDetailAsync(db, id));
        })
        .RequireAuthorization(policy => policy.RequireRole(Roles.Manager))
        .WithSummary("Simula o pagamento do boleto")
        .WithDescription("Faz o papel do retorno do banco: o boleto fica Pago e o pedido passa a Pago, liberado para emitir a nota. Boleto já pago retorna 422. RowVersion velho retorna 409. Requer perfil Manager.")
        .Produces<SalesOrderDetailDTO>(StatusCodes.Status200OK)
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    // IgnoreQueryFilters: produto, cliente e armazém excluídos continuam aparecendo no pedido.
    private static Task<SalesOrderDetailDTO?> LoadDetailAsync(AppDbContext db, Guid id) =>
        db.SalesOrders
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(o => o.Id == id)
            .Select(o => new SalesOrderDetailDTO(
                o.Id,
                o.Number,
                o.Status.ToString(),
                o.PaymentMethod.ToString(),
                o.CustomerId,
                o.Customer.Name,
                o.Customer.Document,
                o.StorageId,
                o.Storage.Name,
                o.Notes,
                o.CreatedBy,
                o.CreatedAt,
                o.RowVersion,
                o.Items
                    .OrderBy(i => i.Product.Name)
                    .Select(i => new SalesOrderItemResponseDTO(
                        i.Id, i.ProductId, i.Product.Name, i.Product.SKU, i.Quantity, i.UnitPrice))
                    .ToList(),
                o.Boleto == null
                    ? null
                    : new BoletoResponseDTO(
                        o.Boleto.Id,
                        o.Boleto.OurNumber,
                        o.Boleto.DigitableLine,
                        o.Boleto.Amount,
                        o.Boleto.DueDate,
                        o.Boleto.Status.ToString(),
                        o.Boleto.CreatedAt,
                        o.Boleto.PaidBy,
                        o.Boleto.PaidAt),
                o.Invoice == null
                    ? null
                    : new SalesOrderInvoiceDTO(o.Invoice.Id, o.Invoice.Number, o.Invoice.CreatedAt)))
            .FirstOrDefaultAsync();

    private static string GetUserEmail(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Email) ?? "desconhecido";

    private static IResult NotFound() => Results.NotFound(new { mensagem = "Pedido de venda não encontrado" });

    public static IResult ToHttpResult(SalesResult result) =>
        result.IsNotFound
            ? NotFound()
            : result.IsConflict
                ? Results.Problem(
                    title: "Conflito de concorrência",
                    detail: result.Error,
                    statusCode: StatusCodes.Status409Conflict)
                : Results.Problem(
                    title: "Operação recusada",
                    detail: result.Error,
                    statusCode: StatusCodes.Status422UnprocessableEntity);
}
