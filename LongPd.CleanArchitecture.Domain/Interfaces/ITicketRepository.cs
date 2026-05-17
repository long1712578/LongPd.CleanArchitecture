using LongPd.CleanArchitecture.Domain.Entities;

namespace LongPd.CleanArchitecture.Domain.Interfaces;

public interface ITicketRepository : IRepository<Ticket>
{
    Task<Ticket?> GetByIdWithLockAsync(Guid ticketId, CancellationToken ct = default);

    Task<List<Ticket>> GetByEventIdAsync(Guid eventId, CancellationToken ct = default);
}
