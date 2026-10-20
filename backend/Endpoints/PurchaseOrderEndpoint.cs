using System.Security.Claims;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProjectBee.Data;
using ProjectBee.DTO.Purchase;
using ProjectBee.Models;
using ProjectBee.Services;

public static class PurchaseOrderEndpointsExtensions
{
    public static void MapPurchaseOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var purchasesApi = app.MapGroup("/api/purchase-orders")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Operator, Roles.Manager))
            .WithTags("Compras");

        purchasesApi.MapGet("/", async (AppDbContext db, PurchaseOrderStatus? status, Guid? supplierId) =>
        {
            // IgnoreQueryFilters: o pedido continua listado mesmo que o fornecedor ou o armazém tenham sido excluídos.
            var query = db.PurchaseOrders.IgnoreQueryFilters().AsNoTracking();

            if (status.HasValue)
                query = query.Where(o => o.Status == status.Value);

            if (supplierId.HasValue)
                query = query.Where(o => o.SupplierId == supplierId.Value);

            var orders = await query
                .OrderByDescending(o => o.Number)
                .Take(200)
                .Select(o => new PurchaseOrderSummaryDTO(
                    o.Id,
                    o.Number,
                    o.Status.ToString(),
                    o.SupplierId,
                    o.Supplier.Name,
                    o.StorageId,
                    o.Storage.Name,
                    o.Items.Sum(i => i.Quantity * i.UnitPrice),
                    o.Items.Sum(i => i.Quantity),
                    o.Items.Sum(i => i.ReceivedQuantity),
                    o.CreatedAt))
                .ToListAsync();

            return Results.Ok(orders);
        })
        .WithSummary("Lista os pedidos de compra")
        .WithDescription("Retorna os 200 pedidos mais recentes, do número maior para o menor, filtráveis por status (Open, Partial, Received, Cancelled) e fornecedor. Total é o valor pedido (quantidade x preço do pedido). Disponível para Operator e Manager.")
        .Produces<List<PurchaseOrderSummaryDTO>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized);

        purchasesApi.MapGet("/{id}", async (AppDbContext db, Guid id) =>
        {
            var order = await LoadDetailAsync(db, id);
            return order == null ? NotFound() : Results.Ok(order);
        })
        .WithSummary("Busca um pedido de compra com itens e recebimentos")
        .WithDescription("Cada item traz a quantidade pedida e a recebida; cada recebimento traz a nota, os lotes e o preço da nota ao lado do preço do pedido. O RowVersion volta no recebimento e no cancelamento.")
        .Produces<PurchaseOrderDetailDTO>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status404NotFound);

        purchasesApi.MapPost("/", async (
            CreatePurchaseOrderDTO dto,
            IValidator<CreatePurchaseOrderDTO> validator,
            PurchaseService service,
            AppDbContext db,
            ClaimsPrincipal user) =>
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var result = await service.CreateOrderAsync(dto, GetUserEmail(user));
            if (!result.Success)
                return ToHttpResult(result);

            return Results.Created($"/api/purchase-orders/{result.OrderId}", await LoadDetailAsync(db, result.OrderId!.Value));
        })
        .RequireAuthorization(policy => policy.RequireRole(Roles.Manager))
        .WithSummary("Cria um pedido de compra")
        .WithDescription("O pedido nasce Aberto (aprovado, aguardando recebimento). Fornecedor, armazém e produtos precisam existir e estar ativos; cada produto aparece uma vez, com quantidade e preço unitário maiores que zero. Requer perfil Manager.")
        .Produces<PurchaseOrderDetailDTO>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status403Forbidden);

        purchasesApi.MapPost("/{id}/receipts", async (
            Guid id,
            ReceivePurchaseOrderDTO dto,
            IValidator<ReceivePurchaseOrderDTO> validator,
            PurchaseService service,
            AppDbContext db,
            ClaimsPrincipal user) =>
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var result = await service.ReceiveAsync(id, dto, GetUserEmail(user));
            if (!result.Success)
                return ToHttpResult(result);

            return Results.Ok(await LoadDetailAsync(db, id));
        })
        .WithSummary("Registra o recebimento de uma nota do pedido")
        .WithDescription("Dá entrada no estoque do armazém do pedido, lote a lote, numa única transação com a nota e o status. Recebimento menor que o pendente deixa o pedido Parcial; o restante o deixa Recebido. Retorna 422 para recebimento maior que o pendente, pedido cancelado ou já recebido, produto ou armazém inativo, nota repetida do mesmo fornecedor e preço da nota diferente do pedido sem PriceDivergenceReason. RowVersion velho retorna 409. Disponível para Operator e Manager.")
        .WithPurchaseResponses();

        purchasesApi.MapPost("/{id}/cancel", async (
            Guid id,
            CancelPurchaseOrderDTO dto,
            IValidator<CancelPurchaseOrderDTO> validator,
            PurchaseService service,
            AppDbContext db,
            ClaimsPrincipal user) =>
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var result = await service.CancelAsync(id, dto, GetUserEmail(user));
            if (!result.Success)
                return ToHttpResult(result);

            return Results.Ok(await LoadDetailAsync(db, id));
        })
        .RequireAuthorization(policy => policy.RequireRole(Roles.Manager))
        .WithSummary("Cancela o pendente de um pedido")
        .WithDescription("Vale para pedidos Abertos e Parciais. No Parcial, só o pendente é cancelado: o que já chegou continua no estoque e as notas seguem registradas. Pedido Recebido ou já Cancelado retorna 422. Motivo obrigatório, de 5 a 250 caracteres. RowVersion velho retorna 409. Requer perfil Manager.")
        .WithPurchaseResponses()
        .Produces(StatusCodes.Status403Forbidden);
    }

    private static RouteHandlerBuilder WithPurchaseResponses(this RouteHandlerBuilder builder) =>
        builder
            .Produces<PurchaseOrderDetailDTO>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

    // IgnoreQueryFilters: produto, fornecedor e armazém excluídos continuam aparecendo no pedido.
    private static Task<PurchaseOrderDetailDTO?> LoadDetailAsync(AppDbContext db, Guid id) =>
        db.PurchaseOrders
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(o => o.Id == id)
            .Select(o => new PurchaseOrderDetailDTO(
                o.Id,
                o.Number,
                o.Status.ToString(),
                o.SupplierId,
                o.Supplier.Name,
                o.Supplier.Document,
                o.StorageId,
                o.Storage.Name,
                o.Notes,
                o.CreatedBy,
                o.CreatedAt,
                o.CancelledBy,
                o.CancelledAt,
                o.CancelReason,
                o.RowVersion,
                o.Items
                    .OrderBy(i => i.Product.Name)
                    .Select(i => new PurchaseOrderItemResponseDTO(
                        i.Id, i.ProductId, i.Product.Name, i.Product.SKU, i.Quantity, i.ReceivedQuantity, i.UnitPrice))
                    .ToList(),
                o.Receipts
                    .OrderBy(r => r.CreatedAt)
                    .Select(r => new PurchaseReceiptResponseDTO(
                        r.Id,
                        r.InvoiceNumber,
                        r.PriceDivergenceReason,
                        r.CreatedBy,
                        r.CreatedAt,
                        r.Items
                            .Select(ri => new PurchaseReceiptItemResponseDTO(
                                ri.PurchaseOrderItemId,
                                ri.PurchaseOrderItem.Product.Name,
                                ri.Batch,
                                ri.Quantity,
                                ri.UnitPrice,
                                ri.PurchaseOrderItem.UnitPrice))
                            .ToList()))
                    .ToList()))
            .FirstOrDefaultAsync();

    private static string GetUserEmail(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Email) ?? "desconhecido";

    private static IResult NotFound() => Results.NotFound(new { mensagem = "Pedido de compra não encontrado" });

    private static IResult ToHttpResult(PurchaseResult result) =>
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
