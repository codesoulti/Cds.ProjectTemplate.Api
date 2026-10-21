namespace Cg.ProjectName.Domain.Interfaces.Shared;

/// <summary>
/// Marca uma entidade que possui um token de concorrência otimista
/// (<see cref="RowVersion"/>). Usado por <c>EfRepository{TEntity,TKey}.Update</c>
/// para saber, em tempo de execução e sem reflection por nome de string, se
/// deve configurar o valor original do token de concorrência a partir do que
/// o cliente enviou — a única forma de detectar uma edição sobre um dado já
/// desatualizado (stale write) quando a mesma requisição já recarregou a
/// entidade do banco antes de aplicar as mudanças.
/// </summary>
public interface IHasRowVersion
{
    byte[] RowVersion { get; set; }
}
