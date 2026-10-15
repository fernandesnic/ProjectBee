using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ProjectBee.Tests.Infrastructure;

// Sobe a API inteira em memória (pipeline, auth, validadores, endpoints) apontando para um banco de teste.
// O ambiente "Testing" não carrega os user secrets: tudo que a API precisa é configurado aqui.
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string DemoEmail = "demo@projectbee.dev";
    public const string DemoPassword = "Demo@Teste123";

    public TestDatabase Database { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration(config => config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = Database.ConnectionString,
            ["Jwt:Key"] = "chave-jwt-usada-apenas-nos-testes-0123456789",
            ["Jwt:Issuer"] = "ProjectBee",
            ["Jwt:Audience"] = "ProjectBee",
            ["Demo:Password"] = DemoPassword,
        }));
    }

    public async Task<HttpClient> CreateManagerClientAsync() =>
        await CreateAuthenticatedClientAsync(DemoEmail, DemoPassword);

    // O /register cria usuários com perfil Operator.
    public async Task<HttpClient> CreateOperatorClientAsync()
    {
        var email = $"operador-{Guid.NewGuid():N}@projectbee.dev";
        const string password = "Operador@123";

        using var anonymous = CreateClient();
        var register = await anonymous.PostAsJsonAsync("/api/auth/register",
            new { nome = "Operador de teste", email, password });
        register.EnsureSuccessStatusCode();

        return await CreateAuthenticatedClientAsync(email, password);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string email, string password)
    {
        var client = CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        login.EnsureSuccessStatusCode();

        var body = await login.Content.ReadFromJsonAsync<LoginResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body!.Token);
        return client;
    }

    public Task InitializeAsync() => Database.InitializeAsync();

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
        await Database.DisposeAsync();
    }

    private sealed record LoginResponse(string Token);
}
