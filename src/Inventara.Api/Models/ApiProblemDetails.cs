using Microsoft.AspNetCore.Mvc;

namespace Inventara.Api.Models;

public sealed class ApiProblemDetails : ProblemDetails
{
    public bool Success { get; init; }

    public object? Data { get; init; }

    public string? Message { get; init; }

    public IReadOnlyDictionary<string, string[]>? Errors { get; init; }
}
