namespace Inventara.Api.Models;

public sealed record ApiResponse<T>(
    bool Success,
    T? Data,
    string? Message,
    object? Errors)
{
    public static ApiResponse<T> Ok(T data, string? message = null) =>
        new(true, data, message, null);
}
