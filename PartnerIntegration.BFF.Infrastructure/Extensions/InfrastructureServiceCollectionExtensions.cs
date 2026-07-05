using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PartnerIntegration.BFF.Core.Interfaces;
using PartnerIntegration.BFF.Infrastructure.HealthChecks;
using PartnerIntegration.BFF.Infrastructure.HttpClients;
using PartnerIntegration.BFF.Infrastructure.Options;
using PartnerIntegration.BFF.Infrastructure.Publishers;
using Polly;
using Microsoft.Extensions.Http.Resilience;
using RabbitMQ.Client;

namespace PartnerIntegration.BFF.Infrastructure.Extensions;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<PartnerApiOptions>()
            .BindConfiguration(PartnerApiOptions.SectionName)
            .Validate(o => !string.IsNullOrWhiteSpace(o.BaseAddress), "PartnerApi:BaseAddress is required.")
            .ValidateOnStart();

        services.AddOptions<RabbitMqOptions>()
            .BindConfiguration(RabbitMqOptions.SectionName)
            .Validate(o => !string.IsNullOrWhiteSpace(o.Uri), "RabbitMQ:Uri is required.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.QueueName), "RabbitMQ:QueueName is required.")
            .ValidateOnStart();

        var partnerApiSection = configuration.GetSection(PartnerApiOptions.SectionName);

        services.AddHttpClient<IPartnerVerificationClient, PartnerVerificationClient>()
            .ConfigureHttpClient(client =>
            {
                var baseAddress = partnerApiSection["BaseAddress"] ?? throw new InvalidOperationException("PartnerApi:BaseAddress is not configured.");
                client.BaseAddress = new Uri(baseAddress);

                if (int.TryParse(partnerApiSection["TimeoutSeconds"], out var timeout))
                    client.Timeout = TimeSpan.FromSeconds(timeout);
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

                builder.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                {
                    SamplingDuration = TimeSpan.FromSeconds(30),
                    FailureRatio = 0.5,
                    MinimumThroughput = 5,
                    BreakDuration = TimeSpan.FromSeconds(15),
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .Handle<TimeoutException>()
                        .Handle<HttpRequestException>()
                        .HandleResult(r => (int)r.StatusCode >= 500)
                });

                builder.AddTimeout(TimeSpan.FromSeconds(10));
            });

        services.AddSingleton<IConnection>(sp =>
        {
            var rabbitOptions = sp.GetRequiredService<IOptions<RabbitMqOptions>>().Value;

            var factory = new ConnectionFactory
            {
                Uri = new Uri(rabbitOptions.Uri),
                AutomaticRecoveryEnabled = true
            };

            return factory.CreateConnectionAsync().GetAwaiter().GetResult();
        });

        services.AddScoped<ITransactionMessagePublisher, TransactionMessagePublisher>();

        services.AddHealthChecks().AddCheck<RabbitMqHealthCheck>("rabbitmq", tags: ["ready"]);

        return services;
    }
}
