using Cg.ProjectName.Application.Interfaces.Shared;
using Cg.ProjectName.Infrastructure.Data.Contexts.EfCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Cg.ProjectName.Infrastructure.Data.UnitOfWork
{
    public class UnitOfWork(CgProjectNameDbContext context) : IUnitOfWork
    {
        private readonly CgProjectNameDbContext _context = context;

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => await _context.SaveChangesAsync(cancellationToken);

        public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => await _context.Database.BeginTransactionAsync(cancellationToken);

        public async Task<TResult> ExecuteInStrategyAsync<TResult>(Func<Task<TResult>> operation)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(operation);
        }

        public void ClearChangeTracker() => _context.ChangeTracker.Clear();
    }
}
