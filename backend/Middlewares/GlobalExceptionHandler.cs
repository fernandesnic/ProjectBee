using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        if (exception is BadHttpRequestException badRequest)
        {
            _logger.LogInformation("Requisição malformada: {Message}", badRequest.Message);

            httpContext.Response.StatusCode = badRequest.StatusCode;
            await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = badRequest.StatusCode,
                Title = "Requisição inválida",
                Detail = "O corpo da requisição não pôde ser lido. Confira se o JSON está bem formado e se cada campo tem o tipo esperado (números sem aspas, datas e IDs no formato certo)."
            }, cancellationToken);

            return true;
        }

        _logger.LogError(exception, "Ocorreu um erro não tratado: {Message}", exception.Message);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Internal Server Error",
            Detail = _env.IsDevelopment()
                ? exception.Message
                : "Ocorreu um erro interno inesperado. Tente novamente mais tarde ou contate o suporte."
        };

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}