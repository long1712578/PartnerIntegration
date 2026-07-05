using PartnerIntegration.BFF.Api.Middlewares;

namespace PartnerIntegration.BFF.Api.Extensions;

public static class ExceptionHandlingExtensions
{
    /// <summary>
    /// Registers the GlobalExceptionMiddleware.
    /// Call this before all other middleware so every exception is caught.
    /// </summary>
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
        => app.UseMiddleware<GlobalExceptionMiddleware>();
}
