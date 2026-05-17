using LongPd.CleanArchitecture.Application.Common;
using MediatR;

namespace LongPd.CleanArchitecture.Application.Abstractions.Messaging;

/// <summary>
/// Marker interface for write operations (change state).
/// Commands return Result{TResponse} — never void, never throw for business errors.
/// Pipeline: LoggingBehavior → ValidationBehavior → Handler
/// </summary>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>;

/// <summary>Command with no meaningful return value (e.g., Delete, Publish).</summary>
public interface ICommand : IRequest<Result>;

// ─── Handler interfaces (makes mocking in tests cleaner) ──────────────────────

public interface ICommandHandler<TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>;

public interface ICommandHandler<TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand;

/// <summary>
/// Marker interface for read operations (no state change).
/// Queries go through: LoggingBehavior → CachingBehavior → Handler
/// Handler uses Dapper (NOT EF Core tracking).
/// </summary>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>;

public interface IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>;
