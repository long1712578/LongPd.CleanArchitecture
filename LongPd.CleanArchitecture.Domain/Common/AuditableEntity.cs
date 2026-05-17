namespace LongPd.CleanArchitecture.Domain.Common;

/// <summary>
/// Extends BaseEntity with automatic audit trail fields.
/// UnitOfWork fills these via ChangeTracker — entities never set them directly.
/// </summary>
public abstract class AuditableEntity : BaseEntity
{
    /// <summary>UTC timestamp set on INSERT.</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>UTC timestamp set on every UPDATE.</summary>
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>User identifier (e.g., JWT sub claim) set by ICurrentUserService on INSERT.</summary>
    public string? CreatedBy { get; private set; }

    /// <summary>User identifier set by ICurrentUserService on UPDATE.</summary>
    public string? UpdatedBy { get; private set; }

    // Called exclusively by UnitOfWork via reflection-free setter approach
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
