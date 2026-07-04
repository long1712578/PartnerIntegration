using PartnerIntegration.BFF.Core.Interfaces;

namespace PartnerIntegration.BFF.Infrastructure.HttpClients
{
    public class PartnerVerificationClient(HttpClient httpClient) : IPartnerVerificationClient
    {
        public async Task<bool> VerifyPartnerAsync(string partnerId, CancellationToken cancellationToken = default)
        {
            var response = await httpClient.GetAsync($"/internal/mock-partner/{partnerId}", cancellationToken);

            return response.IsSuccessStatusCode;
        }
    }
}
