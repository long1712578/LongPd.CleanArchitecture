namespace LongPd.CleanArchitecture.Application.Common;

/// <summary>
/// Application-level abstraction for concurrency conflicts.
/// Infrastructure layer catches DbUpdateConcurrencyException and re-throws this.
/// This decouples Application from EF Core — handlers catch this instead.
/// </summary>
public sealed class ConcurrencyException : Exception
{
    public ConcurrencyException()
        : base("A concurrency conflict occurred. The resource was modified by another request.") { }

    public ConcurrencyException(string message) : base(message) { }

    public ConcurrencyException(string message, Exception innerException)
        : base(message, innerException) { }
}
