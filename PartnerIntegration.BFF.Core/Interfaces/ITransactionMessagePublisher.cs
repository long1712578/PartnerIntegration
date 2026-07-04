using PartnerIntegration.BFF.Core.Models;

namespace PartnerIntegration.BFF.Core.Interfaces;

public interface ITransactionMessagePublisher
{
    Task PublishTransactionAsync(PartnerTransactionRequest transaction, CancellationToken cancellationToken = default);
}
