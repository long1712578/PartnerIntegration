namespace PartnerIntegration.BFF.Core.Interfaces;

public interface IPartnerVerificationClient
{
    Task<bool> VerifyPartnerAsync(string partnerId, CancellationToken cancellationToken = default);
}
