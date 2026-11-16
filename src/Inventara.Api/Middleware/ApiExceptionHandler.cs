using FluentValidation;
using Inventara.Api.Models;
using Inventara.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace Inventara.Api.Middleware;

public sealed class ApiExceptionHandler(
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail, errors) = exception switch
        {
            ValidationException validationException => (
                StatusCodes.Status400BadRequest,
                "Validation failed",
                "One or more validation errors occurred.",
                validationException.Errors
                    .GroupBy(error => error.PropertyName)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Select(error => error.ErrorMessage).Distinct().ToArray())),
            EntityNotFoundException notFoundException => (
                StatusCodes.Status404NotFound,
                "Not Found",
                notFoundException.Message,
                (IReadOnlyDictionary<string, string[]>?)null),
            ConflictException conflictException => (
                StatusCodes.Status409Conflict,
                "Conflict",
                conflictException.Message,
                (IReadOnlyDictionary<string, string[]>?)null),
            InvalidCredentialsException => (
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                "Email or password is incorrect.",
                (IReadOnlyDictionary<string, string[]>?)null),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                "An unexpected error occurred.",
                (IReadOnlyDictionary<string, string[]>?)null)
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(
                "Unhandled API exception at {Path}: {ExceptionType}",
                httpContext.Request.Path,
                exception.GetType().Name);
        }

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(
            new ApiProblemDetails
            {
                Type = $"https://httpstatuses.com/{statusCode}",
                Title = title,
                Status = statusCode,
                Detail = detail,
                Instance = httpContext.Request.Path,
                Success = false,
                Data = null,
                Message = detail,
                Errors = errors
            },
            cancellationToken);

        return true;
    }
}
