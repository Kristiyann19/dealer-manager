namespace DealerManager.Application.IRepository
{
    public interface IUnitOfWork
    {
        Task<int> SaveChanges(CancellationToken cancellationToken);

        // The operation owns the writes in this scope and calls SaveChanges explicitly.
        Task<T> ExecuteInTransaction<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken);
    }
}
