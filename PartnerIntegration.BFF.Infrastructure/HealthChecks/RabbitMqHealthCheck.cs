using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;

namespace PartnerIntegration.BFF.Infrastructure.HealthChecks;

public sealed class RabbitMqHealthCheck(IConnection connection) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(connection.IsOpen
            ? HealthCheckResult.Healthy("RabbitMQ connection is active.")
            : HealthCheckResult.Unhealthy("RabbitMQ connection is closed."));
    }
}
