using System.Security.Claims;
using FluentValidation;
using ProjectBee.Services;

public static class MovementEndpointsExtensions
{
    public static void MapMovementEndpoints(this IEndpointRouteBuilder app)
    {
        var movementsApi = app.MapGroup("/api/movements")
            .RequireAuthorization();

        movementsApi.MapPost("/entry", async (
            CreateEntryDTO dto,
            IValidator<CreateEntryDTO> validator,
            StockMovementService service,
            ClaimsPrincipal user) =>
        {
            var validationResult = await validator.ValidateAsync(dto);

            if (!validationResult.IsValid)
            {
                return Results.ValidationProblem(validationResult.ToDictionary());
            }

            var email = user.FindFirstValue(ClaimTypes.Email)
                        ?? user.FindFirstValue("email")
                        ?? "desconhecido";

            var newBalance = await service.RegisterEntryAsync(
                dto.ProductId, dto.StorageId, dto.Batch, dto.Quantity, email);

            return Results.Ok(new { mensagem = "Entrada registrada com sucesso!", saldoAtual = newBalance });
        });
    }
}