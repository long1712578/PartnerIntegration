using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MassTransit;
using PartnerIntegration.BFF.Core.Interfaces;
using PartnerIntegration.BFF.Infrastructure.Clients;
using PartnerIntegration.BFF.Infrastructure.Publishers;

namespace PartnerIntegration.BFF.Infrastructure.Extensions;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add HTTP Client for Partner Verification
        services.AddHttpClient<IPartnerVerificationClient, PartnerVerificationClient>()
            .ConfigureHttpClient(client =>
            {
                var baseAddress = configuration["PartnerApi:BaseAddress"] ?? string.Empty;
                client.BaseAddress = new Uri(baseAddress);
                client.Timeout = TimeSpan.FromSeconds(int.Parse(configuration["PartnerApi:TimeoutSeconds"] ?? "30"));
            });

        // Add MassTransit for RabbitMQ
        //services.AddMassTransit(x =>
        //{
        //    x.AddConsumers(typeof(InfrastructureServiceCollectionExtensions).Assembly);

        //    x.UsingRabbitMq((context, cfg) =>
        //    {
        //        var rabbitmqUri = configuration["RabbitMQ:Uri"] ?? string.Empty;

        //        cfg.Host(new Uri(rabbitmqUri));

        //        cfg.ConfigureEndpoints(context);
        //    });
        //});

        // Register TransactionMessagePublisher
        //services.AddScoped<ITransactionMessagePublisher, TransactionMessagePublisher>();

        return services;
    }
}
