using Cg.ProjectName.Domain.Interfaces.Repositories;
using Cg.ProjectName.Domain.Options.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cg.ProjectName.Infrastructure.Data.Interceptors;

public sealed class SoftDeleteInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplySoftDelete(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplySoftDelete(eventData.Context);

        return base.SavingChangesAsync(
            eventData,
            result,
            cancellationToken);
    }

    private static void ApplySoftDelete(DbContext? context)
    {
        if (context is null || !SoftDeleteOptions.Enabled)
            return;

        // Usa o contrato não genérico ISoftDelete em vez de
        // EntitySoftDeletable<Guid> — a versão anterior só reconhecia
        // entidades soft-deletable com chave Guid; qualquer entidade futura
        // com outro tipo de chave seria hard-deleted silenciosamente, sem
        // nenhum aviso em tempo de compilação ou execução.
        var deletedEntries = context.ChangeTracker
            .Entries()
            .Where(x =>
                x.State == EntityState.Deleted &&
                x.Entity is ISoftDelete);

        foreach (var entry in deletedEntries)
        {
            entry.State = EntityState.Modified;
            ((ISoftDelete)entry.Entity).Delete();
        }
    }
}