namespace MonatroxPortfolio.Shared.Contracts;

public sealed record ContractError(string Code, string Message);

public sealed class Result<T>
{
    private Result(T? value, ContractError? error) => (Value, Error) = (value, error);

    public T? Value { get; }
    public ContractError? Error { get; }
    public bool IsSuccess => Error is null;

    public static Result<T> Success(T value) => new(value ?? throw new ArgumentNullException(nameof(value)), null);
    public static Result<T> Failure(string code, string message) => new(default, new(code, message));
}
