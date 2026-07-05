namespace PartnerIntegration.BFF.Core.Models;

/// <summary>
/// Response DTO returned when a transaction is successfully accepted and queued.
/// </summary>
public record TransactionAcceptedResponse(string Message, string TransactionReference, DateTimeOffset AcceptedAt);
