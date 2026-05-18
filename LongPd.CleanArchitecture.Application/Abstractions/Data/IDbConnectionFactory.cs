using System.Data;

namespace LongPd.CleanArchitecture.Application.Abstractions.Data;

/// <summary>
/// Factory for creating raw DB connections for Dapper read queries.
/// Defined in Application, implemented in Infrastructure (SqlConnectionFactory).
/// Application layer depends on this abstraction — NOT on any specific ADO.NET provider.
/// </summary>
public interface IDbConnectionFactory
{
    /// <summary>
    /// Creates and returns an asynchronously opened IDbConnection.
    /// Caller is responsible for disposing (use 'using var connection = ...')
    /// </summary>
    Task<IDbConnection> CreateAsync(CancellationToken ct = default);
}
