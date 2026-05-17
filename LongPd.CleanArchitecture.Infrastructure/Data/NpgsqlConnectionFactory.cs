using System.Data;
using LongPd.CleanArchitecture.Application.Abstractions.Data;
using Npgsql;

namespace LongPd.CleanArchitecture.Infrastructure.Data;

/// <summary>
/// PostgreSQL connection factory for Dapper read queries.
/// Creates a new NpgsqlConnection per request (Dapper manages lifetime).
/// Connection string read from IConfiguration["ConnectionStrings:ReadConnection"].
///
/// ADR: We use a SEPARATE read connection string to allow directing reads to
/// a PostgreSQL read-replica in the future without code changes.
/// </summary>
public sealed class NpgsqlConnectionFactory(string connectionString) : IDbConnectionFactory
{
    public IDbConnection Create()
    {
        var connection = new NpgsqlConnection(connectionString);
        connection.Open(); // Open immediately — Dapper expects an open connection
        return connection;
    }
}
