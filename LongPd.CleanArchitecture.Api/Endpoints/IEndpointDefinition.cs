namespace LongPd.CleanArchitecture.Api.Endpoints;

/// <summary>
/// Contract for endpoint registration groups.
/// Each feature implements this to register its routes.
/// Program.cs discovers and registers all implementations automatically.
/// </summary>
public interface IEndpointDefinition
{
    void RegisterEndpoints(IEndpointRouteBuilder app);
}
