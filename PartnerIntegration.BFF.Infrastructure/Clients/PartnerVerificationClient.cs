using Microsoft.Extensions.Logging;
using PartnerIntegration.BFF.Core.Interfaces;

namespace PartnerIntegration.BFF.Infrastructure.Clients;

internal class PartnerVerificationClient : IPartnerVerificationClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PartnerVerificationClient> _logger;

    public PartnerVerificationClient(HttpClient httpClient, ILogger<PartnerVerificationClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> VerifyPartnerAsync(string partnerId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Verifying partner: {PartnerId}", partnerId);

            var response = await _httpClient.GetAsync($"/api/partners/{partnerId}/verify", cancellationToken);
            
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Partner verification failed for {PartnerId}", partnerId);
            return false;
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex, "Partner verification timeout for {PartnerId}", partnerId);
            return false;
        }
    }
}
