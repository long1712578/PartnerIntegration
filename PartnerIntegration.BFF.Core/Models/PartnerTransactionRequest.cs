namespace PartnerIntegration.BFF.Core.Models;

public record PartnerTransactionRequest(string PartnerId, string TransactionReference, decimal Amount, string Currency, DateTimeOffset Timestamp);
