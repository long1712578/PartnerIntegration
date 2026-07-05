using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using PartnerIntegration.BFF.Core.Interfaces;
using PartnerIntegration.BFF.Infrastructure.HttpClients;
using Polly;
using System.Net;

namespace PartnerIntegration.BFF.Tests.VerificationClients;

public class PartnerVerificationClientTests
{
    [Fact]
    public async Task VerifyPartnerAsync_WhenApiFailsTwiceThenSucceeds_ShouldRetryAndReturnTrue()
    {
        var handlerMock = new Mock<HttpMessageHandler>();

        handlerMock.Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TimeoutException("Timeout 1"))
            .ThrowsAsync(new TimeoutException("Timeout 2"))
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var services = new ServiceCollection();
        services.AddLogging();

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
                BackoffType = DelayBackoffType.Constant,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<TimeoutException>()
                    .Handle<HttpRequestException>()
            });
        });

        var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetRequiredService<IPartnerVerificationClient>();

        // Act
        var result = await client.VerifyPartnerAsync("P-1001");

        // Assert
        result.Should().BeTrue("because the 3rd attempt returned 200 OK");

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Exactly(3),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task VerifyPartnerAsync_WhenAllRetriesFail_ShouldReturnFalse()
    {
        var handlerMock = new Mock<HttpMessageHandler>();

        handlerMock.Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TimeoutException("Timeout 1"))
            .ThrowsAsync(new TimeoutException("Timeout 2"))
            .ThrowsAsync(new TimeoutException("Timeout 3"))
            .ThrowsAsync(new TimeoutException("Timeout 4"));

        var services = new ServiceCollection();
        services.AddLogging();

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
                BackoffType = DelayBackoffType.Constant,
                ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                    .Handle<TimeoutException>()
                    .Handle<HttpRequestException>()
            });
        });

        var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetRequiredService<IPartnerVerificationClient>();

        var result = await client.VerifyPartnerAsync("P-1001");

        // Assert
        result.Should().BeFalse("because all retry attempts were exhausted");
    }

    [Fact]
    public async Task VerifyPartnerAsync_WhenImmediateSuccess_ShouldReturnTrueWithoutRetry()
    {
        var handlerMock = new Mock<HttpMessageHandler>();

        handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var services = new ServiceCollection();
        services.AddLogging();

        services.AddHttpClient<IPartnerVerificationClient, PartnerVerificationClient>(client =>
        {
            client.BaseAddress = new Uri("http://localhost");
        })
        .ConfigurePrimaryHttpMessageHandler(() => handlerMock.Object);

        var serviceProvider = services.BuildServiceProvider();
        var client = serviceProvider.GetRequiredService<IPartnerVerificationClient>();

        // Act
        var result = await client.VerifyPartnerAsync("P-1001");

        // Assert
        result.Should().BeTrue();

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }
}
