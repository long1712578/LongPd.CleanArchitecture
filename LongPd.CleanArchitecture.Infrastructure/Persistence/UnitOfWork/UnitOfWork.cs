using LongPd.CleanArchitecture.Application.Abstractions.Services;
using LongPd.CleanArchitecture.Domain.Common;
using LongPd.CleanArchitecture.Domain.Interfaces;
using LongPd.CleanArchitecture.Infrastructure.Persistence.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore.Storage;

namespace LongPd.CleanArchitecture.Infrastructure.Persistence.UnitOfWork;

/// <summary>
/// Unit of Work implementation.
/// Coordinates all write-side repositories under a single DB transaction.
///
/// On SaveChangesAsync:
///   1. Fills audit fields (CreatedAt, UpdatedAt, CreatedBy, UpdatedBy) via ChangeTracker.
///   2. Saves all changes to DB in one transaction.
///   3. Dispatches all collected domain events via MediatR IPublisher.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private readonly IPublisher _publisher;
    private readonly ICurrentUserService _currentUserService;
    private IDbContextTransaction? _currentTransaction;

    public UnitOfWork(
        AppDbContext context,
        IPublisher publisher,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _publisher = publisher;
        _currentUserService = currentUserService;

        // Lazy-initialize repositories — share the same DbContext instance
        Tickets = new TicketRepository(context);
        Events = new EventRepository(context);
    }

    public ITicketRepository Tickets { get; }
    public IEventRepository Events { get; }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        // Step 1: Fill audit fields before saving
        FillAuditFields();

        // Step 2: Collect domain events from all tracked entities BEFORE saving
        var domainEvents = _context.ChangeTracker
            .Entries<BaseEntity>()
            .Where(e => e.Entity.DomainEvents.Count != 0)
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();

        // Step 3: Save to database
        var result = await _context.SaveChangesAsync(ct);

        // Step 4: Clear domain events (prevent double-dispatch on retry)
        foreach (var entry in _context.ChangeTracker.Entries<BaseEntity>())
            entry.Entity.ClearDomainEvents();

        // Step 5: Dispatch domain events AFTER successful save
        // Dispatch happens after DB commit so handlers can safely query new state
        foreach (var domainEvent in domainEvents)
            await _publisher.Publish(domainEvent, ct);

        return result;
    }

    public async Task BeginTransactionAsync(CancellationToken ct = default)
        => _currentTransaction = await _context.Database.BeginTransactionAsync(ct);

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (_currentTransaction is null)
            throw new InvalidOperationException("No active transaction to commit.");

        await _currentTransaction.CommitAsync(ct);
        await _currentTransaction.DisposeAsync();
        _currentTransaction = null;
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_currentTransaction is null) return;

        await _currentTransaction.RollbackAsync(ct);
        await _currentTransaction.DisposeAsync();
        _currentTransaction = null;
    }

    // ─── Private: Audit Fields via ChangeTracker ─────────────────────────────
    private void FillAuditFields()
    {
        var now = DateTime.UtcNow;
        var userId = _currentUserService.UserId;

        foreach (var entry in _context.ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case Microsoft.EntityFrameworkCore.EntityState.Added:
                    entry.Entity.SetCreatedAudit(now, userId);
                    break;

                case Microsoft.EntityFrameworkCore.EntityState.Modified:
                    entry.Entity.SetUpdatedAudit(now, userId);
                    break;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_currentTransaction is not null)
            await _currentTransaction.DisposeAsync();

        await _context.DisposeAsync();
    }
}
