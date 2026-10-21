using Cg.ProjectName.Application.Interfaces.Shared;
using MediatR;

namespace Cg.ProjectName.Infrastructure.CrossCutting.Shared.UnitOfWork;

public class UnitOfWorkBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public UnitOfWorkBehavior(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Só aplica para Commands
        if (request is not ICommand<TResponse>)
            return await next();

        // Necessário desde que EnableRetryOnFailure foi ligado no DbContext
        // (ver InfrastructureModuleInitializer): o EF Core não permite mais
        // uma transação aberta manualmente (BeginTransactionAsync) fora de
        // uma IExecutionStrategy — faria isso falhar com
        // InvalidOperationException na primeira execução, mesmo sem nenhuma
        // falha transitória real acontecer.
        return await _unitOfWork.ExecuteInStrategyAsync(async () =>
        {
            // Precisa ser a primeira linha de cada tentativa: numa falha
            // transiente, a IExecutionStrategy reexecuta este delegate
            // inteiro (incluindo o "next()" abaixo, ou seja, o handler
            // MediatR completo de novo) — sem limpar o ChangeTracker aqui,
            // a entidade "Added"/"Modified" da tentativa anterior continua
            // rastreada e é persistida de novo junto com a nova, duplicando
            // a escrita. Ver comentário em IUnitOfWork.ClearChangeTracker.
            _unitOfWork.ClearChangeTracker();

            await using var transaction =
                await _unitOfWork.BeginTransactionAsync(cancellationToken);

            TResponse response;

            try
            {
                response = await next();

                // Qualquer IIntegrationEventPublisher.PublishAsync chamado
                // dentro do handler acima (via "next()") já foi acumulado no
                // escopo atual pelo MassTransit — é só aqui, no
                // SaveChangesAsync deste DbContext, que o Outbox Transacional
                // (AddEntityFrameworkOutbox + UseBusOutbox, ver
                // RabbitMqConfiguration, só habilitado do lado da WebApi) de
                // fato grava essas mensagens na tabela OutboxMessage — como
                // parte desta MESMA transação. A entrega real ao RabbitMQ só
                // acontece depois do commit abaixo, por um serviço de
                // background que o próprio MassTransit registra. Não há mais
                // nenhuma chamada explícita de dispatch aqui (ver README,
                // seção Mensageria, para o desenho anterior).
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

            return response;
        });
    }
}
