using System.Security.Claims;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ProjectBee.Data;
using ProjectBee.DTO.Invoice;
using ProjectBee.Models;
using ProjectBee.Services;

public static class InvoiceEndpointsExtensions
{
    public static void MapInvoiceEndpoints(this IEndpointRouteBuilder app)
    {
        var invoicesApi = app.MapGroup("/api/invoices")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Operator, Roles.Manager))
            .WithTags("Faturamento");

        invoicesApi.MapGet("/", async (AppDbContext db, Guid? customerId) =>
        {
            var query = Project(db.SalesInvoices.IgnoreQueryFilters().AsNoTracking()
                .Where(i => !customerId.HasValue || i.SalesOrder.CustomerId == customerId.Value)
                .OrderByDescending(i => i.Number)
                .Take(200));

            return Results.Ok(await query.ToListAsync());
        })
        .WithSummary("Lista as notas fiscais emitidas")
        .WithDescription("Retorna as 200 notas mais recentes, do número maior para o menor, filtráveis por cliente. Cada nota traz os lotes de onde a mercadoria saiu. Disponível para Operator e Manager.")
        .Produces<List<InvoiceResponseDTO>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized);

        invoicesApi.MapGet("/{id}", async (AppDbContext db, Guid id) =>
        {
            var invoice = await LoadAsync(db, id);
            return invoice == null
                ? Results.NotFound(new { mensagem = "Nota fiscal não encontrada" })
                : Results.Ok(invoice);
        })
        .WithSummary("Busca uma nota fiscal por ID")
        .Produces<InvoiceResponseDTO>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status404NotFound);

        invoicesApi.MapPost("/", async (
            CreateInvoiceDTO dto,
            IValidator<CreateInvoiceDTO> validator,
            SalesService service,
            AppDbContext db,
            ClaimsPrincipal user) =>
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var result = await service.InvoiceAsync(dto, user.FindFirstValue(ClaimTypes.Email) ?? "desconhecido");
            if (!result.Success)
                return SalesOrderEndpointsExtensions.ToHttpResult(result);

            return Results.Created($"/api/invoices/{result.Id}", await LoadAsync(db, result.Id!.Value));
        })
        .WithSummary("Emite a nota fiscal de um pedido de venda pago")
        .WithDescription("Dá saída no estoque do armazém do pedido, numa única transação com a nota e o status (Faturado). Os lotes saem em FIFO: o que entrou primeiro no armazém sai primeiro, e um item pode sair de mais de um lote. Retorna 422 para pedido com boleto não pago, pedido já faturado, armazém inativo ou saldo insuficiente. RowVersion velho, ou saldo alterado no meio, retorna 409. Disponível para Operator e Manager.")
        .Produces<InvoiceResponseDTO>(StatusCodes.Status201Created)
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status401Unauthorized)
        .Produces(StatusCodes.Status404NotFound)
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static Task<InvoiceResponseDTO?> LoadAsync(AppDbContext db, Guid id) =>
        Project(db.SalesInvoices.IgnoreQueryFilters().AsNoTracking().Where(i => i.Id == id)).FirstOrDefaultAsync();

    // IgnoreQueryFilters em quem chama: produto, cliente e armazém excluídos continuam na nota.
    private static IQueryable<InvoiceResponseDTO> Project(IQueryable<SalesInvoice> invoices) =>
        invoices.Select(i => new InvoiceResponseDTO(
            i.Id,
            i.Number,
            i.SalesOrderId,
            i.SalesOrder.Number,
            i.SalesOrder.CustomerId,
            i.SalesOrder.Customer.Name,
            i.SalesOrder.Customer.Document,
            i.SalesOrder.Storage.Name,
            i.Items.Sum(it => it.Quantity * it.UnitPrice),
            i.CreatedBy,
            i.CreatedAt,
            i.Items
                .OrderBy(it => it.Product.Name).ThenBy(it => it.Batch)
                .Select(it => new InvoiceItemResponseDTO(
                    it.ProductId, it.Product.Name, it.Product.SKU, it.Batch, it.Quantity, it.UnitPrice))
                .ToList()));
}
