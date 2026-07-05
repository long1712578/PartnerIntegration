using System.Net;

namespace PartnerIntegration.BFF.Core.Exceptions;

/// <summary>
/// Base exception for all application-level domain errors.
/// Maps to a specific HTTP status code and optional machine-readable error code.
/// </summary>
public class AppException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public string? ErrorCode { get; }

    public AppException(
        string message,
        HttpStatusCode statusCode = HttpStatusCode.BadRequest,
        string? errorCode = null) : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
    }
}
