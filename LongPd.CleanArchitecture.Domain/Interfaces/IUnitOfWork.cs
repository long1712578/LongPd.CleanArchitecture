using LongPd.CleanArchitecture.Domain.Interfaces;

namespace LongPd.CleanArchitecture.Domain.Interfaces;

/// <summary>
/// Unit of Work — coordinates all write-side repositories under a single transaction.
/// Application layer ONLY knows about this interface; the implementation is in Infrastructure.
///
/// Usage pattern in CommandHandler:
///   var ticket = await _uow.Tickets.GetByIdWithLockAsync(id, ct);
///   ticket.Reserve(count);
///   await _uow.SaveChangesAsync(ct); // auto-audit + domain event dispatch
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    ITicketRepository Tickets { get; }
    IEventRepository Events { get; }

    /// <summary>
    /// Persists all pending changes in a single transaction.
    /// Also responsible for:
    ///   1. Filling audit fields (CreatedAt, UpdatedAt, CreatedBy, UpdatedBy)
    ///   2. Dispatching collected domain events via MediatR IPublisher
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>Begins an explicit transaction for multi-step operations.</summary>
    Task BeginTransactionAsync(CancellationToken ct = default);

    /// <summary>Commits the current transaction.</summary>
    Task CommitTransactionAsync(CancellationToken ct = default);

    /// <summary>Rolls back the current transaction on failure.</summary>
    Task RollbackTransactionAsync(CancellationToken ct = default);
}
