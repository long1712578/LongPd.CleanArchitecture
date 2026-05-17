using System.Linq.Expressions;
using LongPd.CleanArchitecture.Domain.Common;
using LongPd.CleanArchitecture.Domain.Interfaces;
using LongPd.CleanArchitecture.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LongPd.CleanArchitecture.Infrastructure.Persistence.Repositories;

/// <summary>
/// Generic EF Core repository for write operations.
/// ONLY used for Commands — Queries use Dapper directly.
/// Does NOT expose IQueryable — domain queries are domain-specific methods on typed repos.
/// </summary>
public abstract class Repository<TEntity>(AppDbContext context) : IRepository<TEntity>
    where TEntity : BaseEntity
{
    protected readonly AppDbContext Context = context;

    public async Task<TEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await Context.Set<TEntity>().FindAsync([id], ct);

    public async Task AddAsync(TEntity entity, CancellationToken ct = default)
        => await Context.Set<TEntity>().AddAsync(entity, ct);

    public void Update(TEntity entity)
        => Context.Set<TEntity>().Update(entity);

    public void Delete(TEntity entity)
    {
        if (entity is ISoftDelete softDeletable)
        {
            // Soft delete — mark as deleted via domain method
            softDeletable.MarkAsDeleted(DateTime.UtcNow, null); // CreatedBy filled by UnitOfWork
            Context.Set<TEntity>().Update(entity);
        }
        else
        {
            // Hard delete — rare, only for non-ISoftDelete entities
            Context.Set<TEntity>().Remove(entity);
        }
    }

    public async Task<bool> ExistsAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default)
        => await Context.Set<TEntity>().AnyAsync(predicate, ct);
}
