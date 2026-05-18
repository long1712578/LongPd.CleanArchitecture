using LongPd.CleanArchitecture.Application.Abstractions.Caching;
using LongPd.CleanArchitecture.Application.Abstractions.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace LongPd.CleanArchitecture.Infrastructure.Health;

/// <summary>
/// Health check for PostgreSQL database connectivity.
/// Used by Kubernetes liveness/readiness probes.
/// </summary>
public sealed class DatabaseHealthCheck(IDbConnectionFactory dbConnectionFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken ct = default)
    {
        try
        {
            using var connection = await dbConnectionFactory.CreateAsync(ct);

            // Execute a simple query to verify connectivity
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1;";
            await Task.Run(() => command.ExecuteScalar(), ct);

            return HealthCheckResult.Healthy("PostgreSQL is reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("PostgreSQL is unreachable.", ex);
        }
    }
}

/// <summary>
/// Health check for the cache layer (Redis or in-memory).
/// Verifies that the L1/L2 cache is operational.
/// </summary>
public sealed class CacheHealthCheck(ICacheService cacheService) : IHealthCheck
{
    private const string HealthCheckKey = "health:check";

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken ct = default)
    {
        try
        {
            // Write and read a test value
            await cacheService.SetAsync(HealthCheckKey, "ok", TimeSpan.FromSeconds(10), ct);
            var value = await cacheService.GetAsync<string>(HealthCheckKey, ct);

            if (value == "ok")
            {
                await cacheService.RemoveAsync(HealthCheckKey, ct);
                return HealthCheckResult.Healthy("Cache service is operational.");
            }

            return HealthCheckResult.Degraded("Cache write succeeded but read returned unexpected value.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Cache service is not operational.", ex);
        }
    }
}
