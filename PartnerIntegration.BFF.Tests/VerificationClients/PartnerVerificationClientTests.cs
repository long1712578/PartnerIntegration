using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Moq;
using Moq.Protected;
using PartnerIntegration.BFF.Core.Interfaces;
using PartnerIntegration.BFF.Infrastructure.HttpClients;
using Polly;
using System.Net;

namespace PartnerIntegration.BFF.Tests.VerificationClients
{
    public class PartnerVerificationClientTests
    {
        [Fact]
        public async Task VerifyPartnerAsync_WhenApiFailsTwiceThenSucceeds_ShouldRetryAndReturnTrue()
        {
            // Arrange: Mock HttpMessageHandler to mock reponse from api of partner
            var handlerMock = new Mock<HttpMessageHandler>();

            handlerMock.Protected().SetupSequence<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new TimeoutException("Timeout 1 Times"))
                .ThrowsAsync(new TimeoutException("Timeout 2 Times"))
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

            var services = new ServiceCollection();

            services.AddHttpClient<IPartnerVerificationClient, PartnerVerificationClient>(client =>
            {
                client.BaseAddress = new Uri("http://localhost");
            })
            .ConfigurePrimaryHttpMessageHandler(() => handlerMock.Object)
            .AddResilienceHandler("TestResilience", builder =>
            {
                builder.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromMilliseconds(10),
                    BackoffType = DelayBackoffType.Constant
                });
            });

            var serviceProvider = services.BuildServiceProvider();
            var client = serviceProvider.GetRequiredService<IPartnerVerificationClient>();

            // Act
            var result = await client.VerifyPartnerAsync("P-1001");

            // Assert
            result.Should().BeTrue("Because the API 3 times return 200 oke");

            // Verify: the method SendAsync call 3 times (1 times original + 2 times retry)
            handlerMock.Protected().Verify(
                "SendAsync",
                Times.Exactly(3),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            );
        }
    }
}
