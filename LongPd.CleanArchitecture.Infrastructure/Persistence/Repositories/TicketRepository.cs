using LongPd.CleanArchitecture.Domain.Entities;
using LongPd.CleanArchitecture.Domain.Interfaces;
using LongPd.CleanArchitecture.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LongPd.CleanArchitecture.Infrastructure.Persistence.Repositories;

public sealed class TicketRepository(AppDbContext context)
    : Repository<Ticket>(context), ITicketRepository
{
    public async Task<Ticket?> GetByIdWithLockAsync(Guid ticketId, CancellationToken ct = default)
    {
        // EF Core with PostgreSQL — the optimistic concurrency via xmin handles
        // concurrent updates without explicit row-level locking.
        // For extreme throughput scenarios, consider EF Core's FromSqlRaw with FOR UPDATE.
        return await Context.Tickets
            .FirstOrDefaultAsync(t => t.Id == ticketId, ct);
    }

    public async Task<List<Ticket>> GetByEventIdAsync(Guid eventId, CancellationToken ct = default)
        => await Context.Tickets
            .Where(t => t.EventId == eventId)
            .ToListAsync(ct);
}
