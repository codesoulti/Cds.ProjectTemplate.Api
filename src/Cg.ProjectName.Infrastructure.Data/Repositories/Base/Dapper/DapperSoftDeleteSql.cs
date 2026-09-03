using Cg.ProjectName.Domain.Extensions;
using Cg.ProjectName.Domain.Options.Entities;
using Cg.ProjectName.Infrastructure.Data.Extensions.Dapper;

namespace Cg.ProjectName.Infrastructure.Data.Repositories.Base.Dapper;

public static class DapperSoftDeleteSql
{
    public static string Filter<TEntity>(string alias = "")
    {
        if (!SoftDeleteOptions.Enabled)
            return string.Empty;

        if (!SoftDeleteExtensions.IsSoftDeletable<TEntity>())
            return string.Empty;

        return !alias.IsNullOrWhiteSpace() ? $" {alias}.IsDeleted = 0" : $" IsDeleted = 0";
    }
}