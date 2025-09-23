namespace Vira.Shared;

public sealed record Error(string Code, string Message);

public class Result
{
    public bool IsSuccess { get; }
    public Error? Error { get; }
    protected Result(bool ok, Error? err) { IsSuccess = ok; Error = err; }
    public static Result Success() => new(true, null);
    public static Result Failure(string code, string m) => new(false, new(code, m));
}

public sealed class Result<T> : Result
{
    public T? Value { get; }
    private Result(bool ok, T? value, Error? err) : base(ok, err) => Value = value;
    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string code, string m) => new(false, default, new(code, m));
}
