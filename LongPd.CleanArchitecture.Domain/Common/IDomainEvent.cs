using MediatR;

namespace LongPd.CleanArchitecture.Domain.Common;

/// <summary>
/// Marker interface for all domain events.
/// Inherits from MediatR INotification to enable clean pipeline dispatching.
/// </summary>
public interface IDomainEvent : INotification;
