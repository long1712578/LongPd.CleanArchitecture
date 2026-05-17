using LongPd.CleanArchitecture.Application.Abstractions.Services;
using System.Security.Claims;

namespace LongPd.CleanArchitecture.Api.Services;

/// <summary>
/// Reads the current authenticated user from the HTTP context JWT claims.
/// Registered as Scoped — one instance per HTTP request.
/// </summary>
public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor)
    : ICurrentUserService
{
    private readonly ClaimsPrincipal? _user = httpContextAccessor.HttpContext?.User;

    /// <summary>JWT 'sub' claim — unique user ID.</summary>
    public string? UserId =>
        _user?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
        ?? _user?.FindFirst("sub")?.Value;

    /// <summary>JWT 'name' or email claim.</summary>
    public string? UserName =>
        _user?.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
        ?? _user?.FindFirst("email")?.Value;

    public bool IsAuthenticated => _user?.Identity?.IsAuthenticated ?? false;
}
