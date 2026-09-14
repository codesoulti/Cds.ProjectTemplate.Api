using Cg.ProjectName.Domain.Entities.Shared;

namespace Cg.ProjectName.Infrastructure.Data.Extensions.Dapper;

public static class DapperSoftDeleteExtensions
{
    public static bool IsSoftDeletable<T>()
    {
        return IsSoftDeletable(typeof(T));
    }

    public static bool IsSoftDeletable(Type type)
    {
        while (type is not null)
        {
            if (type.IsGenericType &&
                type.GetGenericTypeDefinition() ==
                typeof(EntitySoftDeletable<>))
                //typeof(ISoftDelete))
                
            {
                return true;
            }

            type = type.BaseType!;
        }

        return false;
    }
}