using Microsoft.EntityFrameworkCore;

namespace ProjectBee.Data;

// Resultado que diz se a unidade de trabalho deve ser confirmada. Regra de negócio recusada
// (Success = false) desfaz tudo o que já tinha sido gravado dentro da transação.
public interface IOperationResult
{
    bool Success { get; }
}

// Unit of Work: tudo o que roda dentro de ExecuteAsync é confirmado junto ou desfeito junto,
// mesmo que o trabalho chame SaveChanges mais de uma vez (ex.: recebimento + contas a pagar).
public class UnitOfWork
{
    private readonly AppDbContext _db;

    public UnitOfWork(AppDbContext db)
    {
        _db = db;
    }

    // Com EnableRetryOnFailure, o EF só aceita transação explícita dentro da estratégia de execução:
    // se a conexão cair no meio (Azure SQL acordando), o bloco inteiro roda de novo do zero.
    // Por isso o trabalho precisa fazer as próprias leituras: nada carregado antes sobrevive à falha.
    public Task<T> ExecuteAsync<T>(Func<Task<T>> work) where T : IOperationResult
    {
        var strategy = _db.Database.CreateExecutionStrategy();

        return strategy.ExecuteAsync(async () =>
        {
            // Sem Commit, o Dispose da transação desfaz tudo no banco.
            await using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                var result = await work();

                if (result.Success)
                    await transaction.CommitAsync();
                else
                    _db.ChangeTracker.Clear();

                return result;
            }
            catch
            {
                // Concorrência, banco fora ou erro inesperado: nada fica gravado pela metade, e o
                // contexto volta limpo para a nova tentativa (ou para quem tratar a exceção).
                _db.ChangeTracker.Clear();
                throw;
            }
        });
    }
}
