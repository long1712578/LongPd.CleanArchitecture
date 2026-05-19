using LongPd.CleanArchitecture.Domain.Entities;
using LongPd.CleanArchitecture.Domain.Interfaces;
using LongPd.CleanArchitecture.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LongPd.CleanArchitecture.Infrastructure.Persistence.Repositories;

public sealed class EventRepository(AppDbContext context) : Repository<Event>(context), IEventRepository
{
    public async Task<Event?> GetByIdWithTicketsAsync(Guid eventId, CancellationToken ct = default) => await Context.Events.Include(e => e.Tickets).FirstOrDefaultAsync(e => e.Id == eventId, ct);

    public async Task<bool> ExistsByNameAsync(string name, CancellationToken ct = default)
    {
        var trimmedName = name.Trim();
        return await Context.Events.AnyAsync(e => e.Name.ToLower() == trimmedName.ToLower(), ct);
    }
}
