using Inventara.Api.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace Inventara.Api.Middleware;

public static class ApiAuthenticationEvents
{
    public static async Task WriteUnauthorizedAsync(JwtBearerChallengeContext context)
    {
        context.HandleResponse();
        await WriteAsync(
            context.HttpContext,
            StatusCodes.Status401Unauthorized,
            "Unauthorized",
            "A valid bearer token is required.");
    }

    public static Task WriteForbiddenAsync(ForbiddenContext context) =>
        WriteAsync(
            context.HttpContext,
            StatusCodes.Status403Forbidden,
            "Forbidden",
            "You are not allowed to perform this operation.");

    private static async Task WriteAsync(
        HttpContext context,
        int statusCode,
        string title,
        string detail)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(
            new ApiProblemDetails
            {
                Type = $"https://httpstatuses.com/{statusCode}",
                Title = title,
                Status = statusCode,
                Detail = detail,
                Instance = context.Request.Path,
                Success = false,
                Message = detail
            },
            context.RequestAborted);
    }
}
