using System.Security.Claims;
using FluentValidation;
using ProjectBee.Services;
using ProjectBee.Models;

public static class MovementEndpointsExtensions
{
    public static void MapMovementEndpoints(this IEndpointRouteBuilder app)
    {
        var movementsApi = app.MapGroup("/api/movements")
            .RequireAuthorization();

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
        });

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
        });

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
        });

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
        .RequireAuthorization(policy => policy.RequireRole(Roles.Manager));
    }

    

    private static string GetUserEmail(ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Email) ?? "desconhecido";

    private static IResult ToHttpResult(MovementResult result, string successMessage) =>
        result.Success
            ? Results.Ok(new { mensagem = successMessage, saldoAtual = result.Balance })
            : Results.Problem(
                title: "Movimento recusado",
                detail: result.Error,
                statusCode: StatusCodes.Status422UnprocessableEntity);
}