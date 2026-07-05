using Microsoft.Extensions.Logging;
using PartnerIntegration.BFF.Core.Interfaces;
using PartnerIntegration.BFF.Core.Models;

namespace PartnerIntegration.BFF.Core.Services;

public sealed class TransactionService(
    IPartnerVerificationClient partnerClient,
    ITransactionMessagePublisher messagePublisher,
    ILogger<TransactionService> logger) : ITransactionService
{
    public async Task<TransactionResult> ProcessTransactionAsync(
        PartnerTransactionRequest request, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Processing transaction {TransactionReference} for partner {PartnerId}", request.TransactionReference, request.PartnerId);

        var isPartnerValid = await partnerClient.VerifyPartnerAsync(request.PartnerId, cancellationToken);

        if (!isPartnerValid)
        {
            logger.LogWarning("Partner verification failed for {PartnerId} — transaction {TransactionReference} rejected", request.PartnerId, request.TransactionReference);

            return TransactionResult.PartnerNotVerified();
        }

        await messagePublisher.PublishTransactionAsync(request, cancellationToken);

        logger.LogInformation("Transaction {TransactionReference} accepted and queued for processing", request.TransactionReference);

        return TransactionResult.Accepted(request.TransactionReference);
    }
}
