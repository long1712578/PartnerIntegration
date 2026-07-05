using Microsoft.Extensions.Logging;
using PartnerIntegration.BFF.Core.Interfaces;

namespace PartnerIntegration.BFF.Infrastructure.HttpClients;

public sealed class PartnerVerificationClient(
    HttpClient httpClient,
    ILogger<PartnerVerificationClient> logger) : IPartnerVerificationClient
{
    public async Task<bool> VerifyPartnerAsync(string partnerId, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Verifying partner {PartnerId}", partnerId);

        try
        {
            var response = await httpClient.GetAsync($"/internal/mock-partner/{partnerId}", cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                logger.LogInformation("Partner {PartnerId} verified successfully", partnerId);
                return true;
            }

            logger.LogWarning("Partner verification returned non-success status for {PartnerId}. StatusCode: {StatusCode}", partnerId, (int)response.StatusCode);
            return false;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP request failed while verifying partner {PartnerId}", partnerId);
            return false;
        }
        catch (TimeoutException ex)
        {
            logger.LogError(ex, "Partner verification timed out for {PartnerId} after all retry attempts", partnerId);
            return false;
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogError(ex, "Partner verification request timed out for {PartnerId}", partnerId);
            return false;
        }
    }
}
