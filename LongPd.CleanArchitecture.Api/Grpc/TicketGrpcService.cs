using System.Collections.Concurrent;
using System.Collections.Immutable;
using Dapper;
using Grpc.Core;
using LongPd.CleanArchitecture.Application.Abstractions.Data;
using LongPd.CleanArchitecture.Application.Abstractions.Notifications;

namespace LongPd.CleanArchitecture.Api.Grpc;

/// <summary>
/// gRPC service implementation for real-time ticket availability streaming.
/// React clients connect via gRPC-Web and receive push updates
/// whenever a TicketReservedDomainEvent or TicketCancelledDomainEvent fires.
///
/// Implements ITicketAvailabilityNotifier — registered as singleton in DI,
/// so domain event handlers can push updates without static coupling.
///
/// Thread safety: Uses ConcurrentDictionary with immutable snapshot pattern
/// to safely manage concurrent stream subscriptions and broadcasts.
/// </summary>
public sealed class TicketGrpcServiceImpl(IDbConnectionFactory dbConnectionFactory, ILogger<TicketGrpcServiceImpl> logger)
    : TicketGrpcService.TicketGrpcServiceBase, ITicketAvailabilityNotifier
{
    /// <summary>
    /// Thread-safe stream registry.
    /// Key: EventId, Value: immutable list of active stream writers.
    /// Uses ConcurrentDictionary for lock-free concurrent access.
    /// </summary>
    private static readonly ConcurrentDictionary<Guid, ImmutableList<IServerStreamWriter<AvailabilityUpdate>>> ActiveStreams = new();

    /// <inheritdoc />
    public async Task NotifyAvailabilityChangedAsync(TicketAvailabilityChangedNotification notification, CancellationToken ct = default)
    {
        var update = new AvailabilityUpdate
        {
            TicketId = notification.TicketId.ToString(),
            EventId = notification.EventId.ToString(),
            TierName = notification.TierName,
            AvailableQuantity = notification.AvailableQuantity,
            TotalQuantity = notification.TotalQuantity,
            IsSoldOut = notification.IsSoldOut,
            UpdatedAt = notification.UpdatedAt.ToString("O")
        };

        await PushToStreamsAsync(notification.EventId, update);
    }

    /// <summary>
    /// Server-streaming RPC — keeps connection open and pushes updates.
    /// Client subscribes once, receives updates until it disconnects.
    /// </summary>
    public override async Task StreamAvailability(StreamAvailabilityRequest request, IServerStreamWriter<AvailabilityUpdate> responseStream, ServerCallContext context)
    {
        if (!Guid.TryParse(request.EventId, out var eventId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid EventId format."));

        logger.LogInformation("[gRPC] Client subscribed to availability stream for EventId: {EventId}", eventId);

        // Register this stream atomically
        ActiveStreams.AddOrUpdate(eventId,_ => ImmutableList.Create(responseStream), (_, existing) => existing.Add(responseStream));

        // Send initial snapshot so client has data immediately
        var snapshot = await GetCurrentSnapshotAsync(eventId, context.CancellationToken);
        foreach (var update in snapshot)
            await responseStream.WriteAsync(update, context.CancellationToken);

        // Keep stream alive until client disconnects
        try
        {
            await Task.Delay(Timeout.Infinite, context.CancellationToken);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("[gRPC] Client disconnected from stream for EventId: {EventId}", eventId);
        }
        finally
        {
            // Unregister this stream atomically
            ActiveStreams.AddOrUpdate(eventId, _ => ImmutableList<IServerStreamWriter<AvailabilityUpdate>>.Empty, (_, existing) => existing.Remove(responseStream));

            // Clean up empty entries
            if (ActiveStreams.TryGetValue(eventId, out var streams) && streams.IsEmpty)
                ActiveStreams.TryRemove(eventId, out _);
        }
    }

    /// <summary>
    /// Unary RPC — snapshot of current availability.
    /// </summary>
    public override async Task<AvailabilitySnapshot> GetCurrentAvailability(GetAvailabilityRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.EventId, out var eventId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Invalid EventId."));

        var updates = await GetCurrentSnapshotAsync(eventId, context.CancellationToken);
        var snapshot = new AvailabilitySnapshot { EventId = request.EventId };
        snapshot.Tiers.AddRange(updates);
        return snapshot;
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    private static async Task PushToStreamsAsync(Guid eventId, AvailabilityUpdate update)
    {
        if (!ActiveStreams.TryGetValue(eventId, out var writers))
            return;

        // Take an immutable snapshot — safe to iterate while other threads modify
        var staleWriters = new List<IServerStreamWriter<AvailabilityUpdate>>();
        foreach (var writer in writers)
        {
            try
            {
                await writer.WriteAsync(update);
            }
            catch
            {
                staleWriters.Add(writer);
            }
        }

        // Clean up disconnected clients atomically
        if (staleWriters.Count > 0)
        {
            ActiveStreams.AddOrUpdate(
                eventId,
                _ => ImmutableList<IServerStreamWriter<AvailabilityUpdate>>.Empty,
                (_, existing) =>
                {
                    var updated = existing;
                    foreach (var stale in staleWriters)
                        updated = updated.Remove(stale);
                    return updated;
                });
        }
    }

    private async Task<List<AvailabilityUpdate>> GetCurrentSnapshotAsync(Guid eventId, CancellationToken ct)
    {
        const string sql = """
            SELECT
                t."Id"::text              AS "TicketId",
                t."EventId"::text         AS "EventId",
                t."TierName",
                t."AvailableQuantity",
                t."TotalQuantity",
                (t."AvailableQuantity" = 0) AS "IsSoldOut"
            FROM "Tickets" t
            WHERE t."EventId" = @EventId AND t."IsDeleted" = false
            """;

        using var connection = await dbConnectionFactory.CreateAsync(ct);
        var rows = await connection.QueryAsync<TicketAvailabilityRow>(sql, new { EventId = eventId });

        return rows.Select(r => new AvailabilityUpdate
        {
            TicketId = r.TicketId,
            EventId = r.EventId,
            TierName = r.TierName,
            AvailableQuantity = r.AvailableQuantity,
            TotalQuantity = r.TotalQuantity,
            IsSoldOut = r.IsSoldOut,
            UpdatedAt = DateTime.UtcNow.ToString("O")
        }).ToList();
    }

    private sealed record TicketAvailabilityRow(string TicketId, string EventId, string TierName, int AvailableQuantity, int TotalQuantity, bool IsSoldOut);
}

