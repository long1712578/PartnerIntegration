using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PartnerIntegration.BFF.Core.Interfaces;
using PartnerIntegration.BFF.Infrastructure.HttpClients;
using PartnerIntegration.BFF.Infrastructure.Publishers;
using Polly;
using Microsoft.Extensions.Http.Resilience;

namespace PartnerIntegration.BFF.Infrastructure.Extensions;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add HTTP Client for Partner Verification
        services.AddHttpClient<IPartnerVerificationClient, PartnerVerificationClient>()
            .ConfigureHttpClient(client =>
            {
                var baseAddress = configuration["PartnerApi:BaseAddress"] ?? throw new InvalidOperationException("PartnerApi:BaseAddress is not configured.");
                client.BaseAddress = new Uri(baseAddress);

                if (int.TryParse(configuration["PartnerApi:TimeoutSeconds"], out var timeout)) client.Timeout = TimeSpan.FromSeconds(timeout);
            })
            .AddResilienceHandler("PartnerApiResilience", builder =>
            {
                builder.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = 3,
                    Delay = TimeSpan.FromSeconds(2),
                    BackoffType = DelayBackoffType.Exponential,
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .Handle<TimeoutException>()
                        .Handle<HttpRequestException>()
                        .HandleResult(r => (int)r.StatusCode >= 500)
                });
                builder.AddTimeout(TimeSpan.FromSeconds(10));
            });

        // Validate RabbitMQ configuration at startup (fail-fast)
        var rabbitmqUri = configuration["RabbitMQ:Uri"] ?? throw new InvalidOperationException("RabbitMQ:Uri is not configured.");
        var queueName = configuration["RabbitMQ:QueueName"] ?? throw new InvalidOperationException("RabbitMQ:QueueName is not configured.");

        if (!Uri.TryCreate(rabbitmqUri, UriKind.Absolute, out var validatedUri))
            throw new InvalidOperationException($"RabbitMQ:Uri '{rabbitmqUri}' is not a valid URI.");
        if (string.IsNullOrWhiteSpace(queueName))
            throw new InvalidOperationException("RabbitMQ:QueueName cannot be empty.");

        // Register TransactionMessagePublisher
        services.AddScoped<ITransactionMessagePublisher, TransactionMessagePublisher>();

        return services;
    }
}
