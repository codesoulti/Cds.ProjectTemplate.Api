using Cg.ProjectName.Application.IntegrationEvents;
using Cg.ProjectName.Application.Interfaces.Shared;
using MediatR;

namespace Cg.ProjectName.Infrastructure.CrossCutting.Shared.UnitOfWork;

public class UnitOfWorkBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIntegrationEventPublisher _integrationEventPublisher;

    public UnitOfWorkBehavior(
        IUnitOfWork unitOfWork,
        IIntegrationEventPublisher integrationEventPublisher)
    {
        _unitOfWork = unitOfWork;
        _integrationEventPublisher = integrationEventPublisher;
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

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }

            // Propositalmente FORA do try/catch acima: o commit já foi
            // confirmado neste ponto, então uma falha aqui (ex.: RabbitMQ
            // fora do ar) não pode cair no catch de cima — chamar
            // RollbackAsync numa transação já commitada lança sua própria
            // exceção (mascarando a falha real de publicação) sem desfazer
            // nada, já que o commit é irreversível a partir daqui.
            //
            // A exceção de DispatchAsync ainda se propaga normalmente para
            // fora deste método (a requisição termina em erro, mesmo com o
            // dado já persistido) — não é silenciada. Isso é uma limitação
            // conhecida do outbox "em memória" (ver IIntegrationEventPublisher):
            // um outbox real (tabela própria + publicador separado) evitaria
            // esse descompasso entre "salvo no banco" e "publicado no
            // barramento". Não deve ser reclassificada como falha transiente
            // pela IExecutionStrategy — o SqlServerRetryingExecutionStrategy
            // só reexecuta o delegate inteiro (reexecutando "next()" e
            // arriscando duplicar o INSERT) para exceções que reconhece como
            // transientes do SQL Server, nunca para uma falha de mensageria.
            await _integrationEventPublisher.DispatchAsync(cancellationToken);

            return response;
        });
    }
}
