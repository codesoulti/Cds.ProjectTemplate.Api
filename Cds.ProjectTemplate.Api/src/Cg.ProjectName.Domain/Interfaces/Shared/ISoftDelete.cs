namespace Cg.ProjectName.Domain.Interfaces.Shared;

/// <summary>
/// Contrato não genérico de soft-delete, implementado por
/// <see cref="Entities.Shared.EntitySoftDeletable{T}"/>
/// independente do tipo da chave. Permite que código cross-cutting (ex.:
/// SoftDeleteInterceptor) detecte e execute o soft-delete de qualquer
/// entidade sem precisar conhecer o tipo genérico fechado em tempo de
/// compilação — antes, esse cenário exigia hardcodar o tipo da chave
/// (ex.: EntitySoftDeletable&lt;Guid&gt;), quebrando silenciosamente para
/// qualquer entidade soft-deletable com outro tipo de chave.
/// </summary>
public interface ISoftDelete
{
    bool IsDeleted { get; }

    void Delete();
}

