using System.Linq.Expressions;
using LongPd.CleanArchitecture.Domain.Common;

namespace LongPd.CleanArchitecture.Domain.Interfaces;
public interface IRepository<TEntity> where TEntity : BaseEntity
{
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task AddAsync(TEntity entity, CancellationToken ct = default);

    void Update(TEntity entity);

    void Delete(TEntity entity);

    Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);
}
