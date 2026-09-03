using MediatR;

namespace Cg.ProjectName.Application.Interfaces.Shared;

public interface ICommand<TResponse> : IRequest<TResponse>
{
}
