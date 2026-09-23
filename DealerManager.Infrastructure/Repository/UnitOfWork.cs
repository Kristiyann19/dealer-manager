using DealerManager.Application.IRepository;

using DealerManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using System.Data;

namespace DealerManager.Infrastructure.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly DealerManagerDbContext context;

        public UnitOfWork(DealerManagerDbContext context)
        {
            this.context = context;
        }

        public Task<int> SaveChanges(CancellationToken cancellationToken)
            => context.SaveChangesAsync(cancellationToken);

        // Serializable isolation protects version allocation and status transitions.
        public async Task<T> ExecuteInTransaction<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
        {
            await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
            try
            {
                var result = await operation(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch (Exception exception)
            {
                await transaction.RollbackAsync(CancellationToken.None);
                context.ChangeTracker.Clear();
                var databaseException = exception as PostgresException ?? exception.InnerException as PostgresException;
                if (databaseException?.SqlState is PostgresErrorCodes.SerializationFailure or PostgresErrorCodes.DeadlockDetected)
                    throw new DBConcurrencyException("Data changed concurrently. Retry the complete request.", exception);

                throw;
            }
        }
    }
}
