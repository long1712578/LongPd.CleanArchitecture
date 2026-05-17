using FluentValidation;
using LongPd.CleanArchitecture.Application.Common;
using MediatR;

namespace LongPd.CleanArchitecture.Application.Behaviors;

/// <summary>
/// MediatR pipeline behavior — runs all registered FluentValidation validators.
/// Runs SECOND (after LoggingBehavior, before CachingBehavior).
///
/// On validation failure: returns Result.Failure with a composite error message.
/// Does NOT throw exceptions — keeps the railway-oriented flow intact.
///
/// Note: Only activates when TResponse is Result or Result{T}.
/// Non-Result requests pass through without validation.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (!validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, ct)));

        var failures = validationResults
            .Where(r => r.Errors.Count != 0)
            .SelectMany(r => r.Errors)
            .ToList();

        if (failures.Count == 0)
            return await next();

        // Aggregate all validation errors into a single description
        var errorMessage = string.Join("; ", failures.Select(f => f.ErrorMessage));
        var error = new Error("Validation.Failed", errorMessage);

        // Try to create a failed Result/Result{T} dynamically
        var responseType = typeof(TResponse);

        if (responseType == typeof(Result))
            return (TResponse)(object)Result.Failure(error);

        if (responseType.IsGenericType && responseType.GetGenericTypeDefinition() == typeof(Result<>))
        {
            var resultType = typeof(Result<>).MakeGenericType(responseType.GetGenericArguments()[0]);
            var failureMethod = resultType.GetMethod(nameof(Result<object>.Failure))!;
            return (TResponse)failureMethod.Invoke(null, [error])!;
        }

        // Fallback: throw ValidationException for non-Result responses (rare)
        throw new ValidationException(failures);
    }
}
