using LongPd.CleanArchitecture.Domain.Interfaces;

namespace LongPd.CleanArchitecture.Domain.Interfaces;

public interface IUnitOfWork : IAsyncDisposable
{
    ITicketRepository Tickets { get; }
    IEventRepository Events { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);

    Task BeginTransactionAsync(CancellationToken ct = default);

    Task CommitTransactionAsync(CancellationToken ct = default);

    Task RollbackTransactionAsync(CancellationToken ct = default);
}
