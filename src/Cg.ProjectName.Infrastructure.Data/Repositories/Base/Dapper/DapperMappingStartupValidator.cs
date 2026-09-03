using Cg.ProjectName.Domain.Entities.Demos;

namespace Cg.ProjectName.Infrastructure.Data.Repositories.Base.Dapper;

/// <summary>
/// Força a resolução (chave primária + colunas) de todo
/// <see cref="DapperMapping{TEntity, TKey}"/> usado pela aplicação durante o
/// startup, em vez de deixar isso acontecer de forma lazy na primeira
/// requisição que tocar o repositório. Sem isso, um erro de configuração
/// (ex.: entidade sem convenção de chave reconhecível) só derruba a
/// aplicação quando o primeiro usuário faz a query — em produção, isso pode
/// levar horas para ser notado.
/// </summary>
public static class DapperMappingStartupValidator
{
    /// <summary>
    /// Adicione aqui um par (TEntity, TKey) para cada nova entidade mapeada
    /// via Dapper (DapperRepository&lt;TEntity, TKey&gt; ou consultas
    /// manuais que usam DapperMapping/DapperSqlBuilder diretamente).
    /// </summary>
    public static void ValidateAll()
    {
        _ = DapperMapping<DemoEmployee, Guid>.Instance;
        _ = DapperMapping<DemoOffice, Guid>.Instance;
    }
}
