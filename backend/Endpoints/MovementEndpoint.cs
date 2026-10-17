using System.Security.Claims;
using FluentValidation;
using ProjectBee.Services;
using ProjectBee.Models;
using ProjectBee.DTO.Movement;
using Microsoft.EntityFrameworkCore;
using ProjectBee.Data;

public static class MovementEndpointsExtensions
{
    public static void MapMovementEndpoints(this IEndpointRouteBuilder app)
    {
        var movementsApi = app.MapGroup("/api/movements")
            .RequireAuthorization()
            .WithTags("Movimentações");
            
        movementsApi.MapGet("/", async (
            AppDbContext db,
            Guid? productId,
            Guid? storageId,
            string? batch) =>
        {
            // O histórico é permanente: inclui movimentos de produtos e armazéns excluídos.
            var query = db.StockMovements.IgnoreQueryFilters().AsNoTracking();

            if (productId.HasValue)
                query = query.Where(m => m.ProductId == productId.Value);

            if (storageId.HasValue)
                query = query.Where(m => m.StorageId == storageId.Value);

            if (!string.IsNullOrWhiteSpace(batch))
                query = query.Where(m => m.Batch == batch);

            var movements = await query
                .OrderByDescending(m => m.CreatedAt)
                .Take(200)
                .Select(m => new MovementResponseDTO(
                    m.Id,
                    m.Type.ToString(),
                    m.ProductId,
                    m.Product.Name,
                    m.StorageId,
                    m.Storage.Name,
                    m.Batch,
                    m.Quantity,
                    m.Reason,
                    m.TransferId,
                    m.CreatedBy,
                    m.CreatedAt))
                .ToListAsync();

            return Results.Ok(movements);
        })
        .WithSummary("Lista o histórico de movimentos")
        .WithDescription("Retorna os 200 movimentos mais recentes, do mais novo para o mais antigo, filtráveis por produto, armazém e lote. Inclui movimentos de produtos e armazéns excluídos. Disponível para Operator e Manager.")
        .Produces<List<MovementResponseDTO>>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status401Unauthorized);
        movementsApi.MapPost("/entry", async (
            StockMovementDTO dto,
            IValidator<StockMovementDTO> validator,
            StockMovementService service,
            ClaimsPrincipal user) =>
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var result = await service.RegisterEntryAsync(
                dto.ProductId, dto.StorageId, dto.Batch, dto.Quantity, GetUserEmail(user));

            return ToHttpResult(result, "Entrada registrada com sucesso!");
        })
        .WithSummary("Registra uma entrada")
        .WithDescription("Soma a quantidade ao saldo do lote, criando o lote se ele ainda não existir. Produto e armazém precisam existir e estar ativos. Disponível para Operator e Manager.")
        .WithMovementResponses(includeBusinessRule: false);

        movementsApi.MapPost("/exit", async (
            StockMovementDTO dto,
            IValidator<StockMovementDTO> validator,
            StockMovementService service,
            ClaimsPrincipal user) =>
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var result = await service.RegisterExitAsync(
                dto.ProductId, dto.StorageId, dto.Batch, dto.Quantity, GetUserEmail(user));

            return ToHttpResult(result, "Saída registrada com sucesso!");
        })
        .WithSummary("Registra uma saída")
        .WithDescription("Subtrai a quantidade do saldo do lote. Saldo insuficiente retorna 422 com o disponível e o solicitado. Disponível para Operator e Manager.")
        .WithMovementResponses();

        movementsApi.MapPost("/transfer", async (
            TransferDTO dto,
            IValidator<TransferDTO> validator,
            StockMovementService service,
            ClaimsPrincipal user) =>
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var result = await service.RegisterTransferAsync(
                dto.ProductId, dto.FromStorageId, dto.ToStorageId, dto.Batch, dto.Quantity, GetUserEmail(user));

            return ToHttpResult(result, "Transferência registrada com sucesso!");
        })
        .WithSummary("Transfere entre armazéns")
        .WithDescription("Grava uma saída na origem e uma entrada no destino, ligadas pelo mesmo TransferId. Origem e destino devem ser diferentes; o lote é criado no destino se não existir. Saldo insuficiente na origem retorna 422. Disponível para Operator e Manager.")
        .WithMovementResponses();

        movementsApi.MapPost("/adjustment", async (
            AdjustmentDTO dto,
            IValidator<AdjustmentDTO> validator,
            StockMovementService service,
            ClaimsPrincipal user) =>
        {
            var validation = await validator.ValidateAsync(dto);
            if (!validation.IsValid)
                return Results.ValidationProblem(validation.ToDictionary());

            var result = await service.RegisterAdjustmentAsync(
                dto.ProductId, dto.StorageId, dto.Batch, dto.CountedQuantity, dto.Reason, GetUserEmail(user));

            return ToHttpResult(result, "Ajuste registrado com sucesso!");
        })
        .RequireAuthorization(policy => policy.RequireRole(Roles.Manager))
        .WithSummary("Registra um ajuste de inventário")
        .WithDescription("Recebe a quantidade contada (não a diferença) e lança a diferença em relação ao saldo atual. Ajuste sem diferença retorna 422. Motivo obrigatório, de 5 a 250 caracteres. Requer perfil Manager.")
        .WithMovementResponses()
        .Produces(StatusCodes.Status403Forbidden);
    }

    // 409 vem do controle de concorrência; 422 só existe quando há regra de negócio que pode recusar o movimento.
    private static RouteHandlerBuilder WithMovementResponses(this RouteHandlerBuilder builder, bool includeBusinessRule = true)
    {
        builder
            .Produces<MovementResultDTO>(StatusCodes.Status200OK)
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status409Conflict);

        if (includeBusinessRule)
            builder.ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        return builder;
    }


    private static string GetUserEmail(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Email) ?? "desconhecido";

    private static IResult ToHttpResult(MovementResult result, string successMessage) =>
        result.Success
            ? Results.Ok(new MovementResultDTO(successMessage, result.Balance))
            : result.IsConflict
                ? Results.Problem(
                    title: "Conflito de concorrência",
                    detail: result.Error,
                    statusCode: StatusCodes.Status409Conflict)
                : Results.Problem(
                    title: "Movimento recusado",
                    detail: result.Error,
                    statusCode: StatusCodes.Status422UnprocessableEntity);
}
