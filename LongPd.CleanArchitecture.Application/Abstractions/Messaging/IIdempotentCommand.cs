namespace LongPd.CleanArchitecture.Application.Abstractions.Messaging;

/// <summary>
/// Marker interface for commands that require idempotency.
/// Implemented by operations that should only execute once per IdempotencyKey
/// (e.g., ticket reservation, payment processing).
/// </summary>
public interface IIdempotentCommand
{
    /// <summary>
    /// A unique identifier provided by the client to ensure the request is processed exactly once.
    /// </summary>
    Guid RequestId { get; }
}
