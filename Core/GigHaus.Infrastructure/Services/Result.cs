namespace GigHaus.Infrastructure.Services;

/// <summary>
/// The outcome of an operation that can fail for an expected reason.
/// <see cref="Error"/> is safe to show to the user.
/// </summary>
public sealed record Result<T>
{
    public bool Success { get; private init; }
    public T? Value { get; private init; }
    public string? Error { get; private init; }

    public static Result<T> Ok(T value) => new() { Success = true, Value = value };
    public static Result<T> Fail(string error) => new() { Error = error };
}
