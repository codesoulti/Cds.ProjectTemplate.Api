using Microsoft.EntityFrameworkCore.Storage;

namespace Cg.ProjectName.Application.Interfaces.Shared;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Executa <paramref name="operation"/> através da
    /// <c>IExecutionStrategy</c> configurada no DbContext (ver
    /// EnableRetryOnFailure em InfrastructureModuleInitializer). Obrigatório
    /// para qualquer fluxo que abra sua própria transação
    /// (BeginTransactionAsync): a partir do momento em que o retry automático
    /// está habilitado, o EF Core não permite mais uma transação
    /// iniciada manualmente fora de uma execution strategy — lançaria
    /// InvalidOperationException na primeira execução.
    /// </summary>
    Task<TResult> ExecuteInStrategyAsync<TResult>(Func<Task<TResult>> operation);

    /// <summary>
    /// Limpa o ChangeTracker do DbContext. OBRIGATÓRIO no início de cada
    /// tentativa dentro de <see cref="ExecuteInStrategyAsync{TResult}"/>:
    /// quando o SQL Server falha transitoriamente durante o SaveChangesAsync
    /// de uma tentativa, a IExecutionStrategy reexecuta o delegate inteiro —
    /// incluindo o handler MediatR completo (o "next()" do pipeline) — mas,
    /// sem este Clear(), o ChangeTracker ainda contém as entidades marcadas
    /// como Added/Modified pela tentativa anterior. Para uma entidade com
    /// chave gerada no cliente (Guid.NewGuid(), como DemoEmployee.Create),
    /// isso não gera conflito de chave — apenas gera uma SEGUNDA linha no
    /// banco a partir de uma única requisição do cliente, um bug de
    /// duplicação silenciosa. Ver EF Core docs sobre "Connection Resiliency":
    /// https://learn.microsoft.com/ef/core/miscellaneous/connection-resiliency
    /// </summary>
    void ClearChangeTracker();
}
