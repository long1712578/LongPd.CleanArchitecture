namespace LongPd.CleanArchitecture.Application.Abstractions.Services;

/// <summary>
/// Abstracts the current authenticated user context.
/// Implementation lives in Api layer (reads from IHttpContextAccessor → JWT claims).
/// Injected into UnitOfWork to fill CreatedBy / UpdatedBy audit fields automatically.
/// </summary>
public interface ICurrentUserService
{
    /// <summary>
    /// The user's unique identifier from the JWT 'sub' claim.
    /// Returns null if the request is unauthenticated (anonymous).
    /// </summary>
    string? UserId { get; }

    /// <summary>User's display name or email from JWT claims.</summary>
    string? UserName { get; }

    /// <summary>True if the current request has a valid authenticated principal.</summary>
    bool IsAuthenticated { get; }
}
