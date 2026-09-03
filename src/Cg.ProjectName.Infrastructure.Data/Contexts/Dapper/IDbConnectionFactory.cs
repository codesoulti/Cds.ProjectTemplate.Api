using System.Data;

namespace Cg.ProjectName.Infrastructure.Data.Contexts.Dapper;

/// <summary>
/// Fábrica de conexões ADO.NET usada pelos repositórios de leitura (Dapper).
/// Cada chamada retorna uma conexão nova; o consumidor é responsável pelo dispose (using).
/// </summary>
public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
