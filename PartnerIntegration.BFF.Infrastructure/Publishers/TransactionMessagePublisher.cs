using Microsoft.Extensions.Logging;
using MassTransit;
using PartnerIntegration.BFF.Core.Interfaces;
using PartnerIntegration.BFF.Core.Models;

namespace PartnerIntegration.BFF.Infrastructure.Publishers;

internal class TransactionMessagePublisher : ITransactionMessagePublisher
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<TransactionMessagePublisher> _logger;

    public TransactionMessagePublisher(IPublishEndpoint publishEndpoint, ILogger<TransactionMessagePublisher> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task PublishTransactionAsync(PartnerTransactionRequest transaction, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Publishing transaction: {TransactionReference}", transaction.TransactionReference);
            
            await _publishEndpoint.Publish(transaction, cancellationToken);
            
            _logger.LogInformation("Transaction published successfully: {TransactionReference}", transaction.TransactionReference);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish transaction: {TransactionReference}", transaction.TransactionReference);
            throw;
        }
    }
}
