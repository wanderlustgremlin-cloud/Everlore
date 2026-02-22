namespace Everlore.Application.Common.Models;

public enum ResultErrorType
{
    Validation,
    NotFound,
    Conflict,
    Unauthorized,
    Forbidden
}

public record ResultError(ResultErrorType Type, string Message);

public class Result
{
    public bool IsSuccess { get; }
    public ResultError? Error { get; }
    public bool IsFailure => !IsSuccess;

    protected Result(bool isSuccess, ResultError? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, null);
    public static Result Failure(ResultErrorType type, string message) => new(false, new(type, message));
    public static Result<T> Success<T>(T value) => new(value, true, null);
    public static Result<T> Failure<T>(ResultErrorType type, string message) => new(default, false, new(type, message));
}

public class Result<T> : Result
{
    public T? Value { get; }

    internal Result(T? value, bool isSuccess, ResultError? error) : base(isSuccess, error)
    {
        Value = value;
    }
}
