using System.Net;

namespace PartnerIntegration.BFF.Core.Exceptions;

/// <summary>
/// Thrown when a requested resource cannot be found.
/// Maps to HTTP 404 Not Found.
/// </summary>
public sealed class NotFoundException : AppException
{
    public NotFoundException(string resourceName, object key)
        : base($"{resourceName} with id '{key}' was not found.", HttpStatusCode.NotFound) { }
}
