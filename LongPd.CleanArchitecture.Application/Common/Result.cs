namespace LongPd.CleanArchitecture.Application.Common;

/// <summary>
/// Represents the success or failure result of an operation.
/// Use Result instead of throwing exceptions for expected business failures.
///
/// RULE: Commands/Queries NEVER throw for business errors — they return Result.Failure.
/// Only infrastructure failures (DB timeout, network) should bubble as exceptions.
/// </summary>
public sealed class Result
{
    private Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException("A successful result cannot have an error.");
        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException("A failed result must have an error.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);

    /// <summary>Converts to Result{T} by wrapping the value on success.</summary>
    public static Result<TValue> Success<TValue>(TValue value) => Result<TValue>.Success(value);
    public static Result<TValue> Failure<TValue>(Error error) => Result<TValue>.Failure(error);
}

/// <summary>
/// Typed result — carries a value on success or an Error on failure.
/// </summary>
public sealed class Result<TValue>
{
    private readonly TValue? _value;

    private Result(bool isSuccess, TValue? value, Error error)
    {
        IsSuccess = isSuccess;
        _value = value;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value of a failed result.");

    public static Result<TValue> Success(TValue value) => new(true, value, Error.None);
    public static Result<TValue> Failure(Error error) => new(false, default, error);

    /// <summary>Pattern match helper — maps success/failure to a unified return type.</summary>
    public TResult Match<TResult>(
        Func<TValue, TResult> onSuccess,
        Func<Error, TResult> onFailure)
        => IsSuccess ? onSuccess(Value) : onFailure(Error);

    /// <summary>Implicit conversion from value — allows: return myValue; in handler returning Result{T}</summary>
    public static implicit operator Result<TValue>(TValue value) => Success(value);
    public static implicit operator Result<TValue>(Error error) => Failure(error);
}
