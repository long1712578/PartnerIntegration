using PartnerIntegration.BFF.Core.Models;

namespace PartnerIntegration.BFF.Core.Services;

/// <summary>
/// Application service for processing partner transactions.
/// Orchestrates partner verification and message publishing.
/// </summary>
public interface ITransactionService
{
    Task<TransactionResult> ProcessTransactionAsync(PartnerTransactionRequest request, CancellationToken cancellationToken = default);
}
