using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using ProjectBee.Data;

namespace ProjectBee.Tests.Infrastructure;

// Banco SQL Server de verdade, com nome único, criado pelas migrations e apagado no fim.
// Precisa ser SQL Server: o RowVersion (concorrência otimista) não existe no InMemory nem no SQLite.
// Servidor padrão: localhost com autenticação do Windows. Para outro (ex.: CI), defina PROJECTBEE_TEST_SQL.
public sealed class TestDatabase : IAsyncLifetime
{
    private const string DefaultServer = "Server=localhost;Trusted_Connection=True;TrustServerCertificate=True;";

    public string ConnectionString { get; }

    public TestDatabase()
    {
        var server = Environment.GetEnvironmentVariable("PROJECTBEE_TEST_SQL") ?? DefaultServer;
        ConnectionString = new SqlConnectionStringBuilder(server)
        {
            InitialCatalog = $"ProjectBeeTests_{Guid.NewGuid():N}"
        }.ConnectionString;
    }

    public AppDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(ConnectionString).Options);

    public async Task InitializeAsync()
    {
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await using var db = CreateContext();
        await db.Database.EnsureDeletedAsync();
    }
}
