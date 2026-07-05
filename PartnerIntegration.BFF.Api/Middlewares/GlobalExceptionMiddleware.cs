using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PartnerIntegration.BFF.Core.Exceptions;

namespace PartnerIntegration.BFF.Api.Middlewares;

public sealed class GlobalExceptionMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionMiddleware> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception on {Method} {Path}: {Message}",
                context.Request.Method, context.Request.Path, ex.Message);

            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title, detail) = exception switch
        {
            AppException ex              => ((int)ex.StatusCode, "Application Error", ex.Message),
            UnauthorizedAccessException  => (StatusCodes.Status401Unauthorized, "Unauthorized", "You are not authorized to perform this action."),
            KeyNotFoundException         => (StatusCodes.Status404NotFound, "Resource Not Found", exception.Message),
            _                            => (StatusCodes.Status500InternalServerError, "Internal Server Error", "An unexpected error occurred.")
        };

        var problemDetails = new ProblemDetails
        {
            Status   = statusCode,
            Title    = title,
            Detail   = detail,
            Instance = context.Request.Path,
            Extensions =
            {
                ["traceId"]   = context.TraceIdentifier,
                ["timestamp"] = DateTimeOffset.UtcNow
            }
        };

        if (exception is AppException { ErrorCode: not null } appEx)
            problemDetails.Extensions["errorCode"] = appEx.ErrorCode;

        context.Response.StatusCode  = statusCode;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsync(JsonSerializer.Serialize(problemDetails, JsonOptions));
    }
}
