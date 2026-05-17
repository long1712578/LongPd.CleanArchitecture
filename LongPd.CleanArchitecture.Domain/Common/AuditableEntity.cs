namespace LongPd.CleanArchitecture.Domain.Common;

/// <summary>
/// Extends BaseEntity with automatic audit trail fields.
/// UnitOfWork fills these via ChangeTracker — entities never set them directly.
/// </summary>
public abstract class AuditableEntity : BaseEntity
{
    public DateTime CreatedAt { get; private set; }

    public DateTime? UpdatedAt { get; private set; }

    public string? CreatedBy { get; private set; }

    public string? UpdatedBy { get; private set; }

    public void SetCreatedAudit(DateTime createdAt, string? createdBy)
    {
        CreatedAt = createdAt;
        CreatedBy = createdBy;
    }

    public void SetUpdatedAudit(DateTime updatedAt, string? updatedBy)
    {
        UpdatedAt = updatedAt;
        UpdatedBy = updatedBy;
    }
}
