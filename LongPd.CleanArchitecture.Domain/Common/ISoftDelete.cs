namespace LongPd.CleanArchitecture.Domain.Common;

/// <summary>
/// Marker interface for soft-delete entities.
/// EF Core global query filter: HasQueryFilter(e => !e.IsDeleted)
/// </summary>
public interface ISoftDelete
{
    bool IsDeleted { get; }
    DateTime? DeletedAt { get; }
    string? DeletedBy { get; }

    void MarkAsDeleted(DateTime deletedAt, string? deletedBy);
}
