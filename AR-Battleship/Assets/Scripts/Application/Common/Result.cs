namespace Core.Application.Common;

public sealed class Result<TValue, TError>
{
    private readonly TValue? _value;
    private readonly TError? _error;

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public TValue Value =>
        IsSuccess
            ? _value!
            : throw new InvalidOperationException("Cannot access Value on a failure result.");

    public TError Error =>
        IsFailure
            ? _error!
            : throw new InvalidOperationException("Cannot access Error on a success result.");

    private Result(bool isSuccess, TValue? value, TError? error)
    {
        IsSuccess = isSuccess;
        _value = value;
        _error = error;
    }

    public static Result<TValue, TError> Success(TValue value)
    {
        return new Result<TValue, TError>(true, value, default);
    }

    public static Result<TValue, TError> Failure(TError error)
    {
        return new Result<TValue, TError>(false, default, error);
    }
}