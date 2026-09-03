using Cg.ProjectName.Domain.Entities.Shared;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Cg.ProjectName.Infrastructure.Data.Extensions;

public static class SoftDeleteModelBuilderExtensions
{
    public static ModelBuilder ApplySoftDelete(
        this ModelBuilder modelBuilder,
        bool enabled)
    {
        if (!enabled)
            return modelBuilder;

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (!IsSoftDeletable(entityType.ClrType))
                continue;

            var parameter = Expression.Parameter(
                entityType.ClrType,
                "e");

            var property = Expression.Property(
                parameter,
                nameof(EntitySoftDeletable<int>.IsDeleted));

            var condition = Expression.Equal(
                property,
                Expression.Constant(false));

            var lambda = Expression.Lambda(
                condition,
                parameter);

            entityType.SetQueryFilter(lambda);
        }

        return modelBuilder;
    }

    private static bool IsSoftDeletable(Type type)
    {
        while (type is not null)
        {
            if (type.IsGenericType &&
                type.GetGenericTypeDefinition() ==
                typeof(EntitySoftDeletable<>))
            {
                return true;
            }

            type = type.BaseType!;
        }

        return false;
    }
}