using System.Data;
using Microsoft.Data.SqlClient;

namespace Cg.ProjectName.Infrastructure.Data.Contexts.Dapper;

public class SqlConnectionFactory : IDbConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string não pode ser vazia.", nameof(connectionString));
        }

        _connectionString = connectionString;
    }

    public IDbConnection CreateConnection() => new SqlConnection(_connectionString);
}
