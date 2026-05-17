namespace LongPd.CleanArchitecture.Domain.Exceptions;

/// <summary>
/// Base exception for all domain rule violations.
/// Should never be caught at infrastructure/application boundaries — convert to Result.Failure instead.
/// Thrown only when an entity's invariant is violated (e.g., reserving more tickets than available).
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }

    public DomainException(string message, Exception innerException)
        : base(message, innerException) { }
}
