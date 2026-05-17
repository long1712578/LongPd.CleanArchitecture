using System.Linq.Expressions;
using LongPd.CleanArchitecture.Domain.Common;

namespace LongPd.CleanArchitecture.Domain.Interfaces;

/// <summary>
/// Generic repository interface — exposes ONLY what Application layer needs.
/// IMPORTANT: Does NOT expose IQueryable — that would leak EF Core concerns upward.
/// Queries should use Dapper read repositories instead.
/// </summary>
/// <typeparam name="TEntity">Must be a BaseEntity.</typeparam>
public interface IRepository<TEntity> where TEntity : BaseEntity
{
    /// <summary>Finds an entity by its primary key. Returns null if not found.</summary>
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Adds a new entity to the change tracker (not persisted until SaveChangesAsync).</summary>
    Task AddAsync(TEntity entity, CancellationToken ct = default);

    /// <summary>Marks entity for update in the change tracker.</summary>
    void Update(TEntity entity);

    /// <summary>
    /// Soft deletes — sets IsDeleted = true via entity method.
    /// For entities NOT implementing ISoftDelete, performs hard delete.
    /// </summary>
    void Delete(TEntity entity);

    /// <summary>Checks if any entity matching the predicate exists.</summary>
    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);
}
