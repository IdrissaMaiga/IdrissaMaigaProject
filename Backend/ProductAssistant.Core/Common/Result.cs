namespace ProductAssistant.Core.Common;

/// <summary>
/// Represents the result of an operation that can either succeed or fail
/// </summary>
public class Result
{
    public bool IsSuccess { get; private set; }
    public bool IsFailure => !IsSuccess;
    public string? ErrorMessage { get; private set; }
    public string? ErrorCode { get; private set; }

    protected Result(bool isSuccess, string? errorMessage = null, string? errorCode = null)
    {
        IsSuccess = isSuccess;
        ErrorMessage = errorMessage;
        ErrorCode = errorCode;
    }

    public static Result Success() => new(true);
    public static Result Failure(string errorMessage, string? errorCode = null) => new(false, errorMessage, errorCode);
    public static Result<T> Success<T>(T value) => new(value, true);
    public static Result<T> Failure<T>(string errorMessage, string? errorCode = null) => new(default!, false, errorMessage, errorCode);
}

/// <summary>
/// Represents the result of an operation that returns a value
/// </summary>
public class Result<T> : Result
{
    public T Value { get; private set; }

    internal Result(T value, bool isSuccess, string? errorMessage = null, string? errorCode = null)
        : base(isSuccess, errorMessage, errorCode)
    {
        Value = value;
    }

    public static implicit operator Result<T>(T value) => Success(value);
}

