namespace PartnerIntegration.BFF.Infrastructure.Options;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMQ";

    public required string Uri { get; init; }
    public required string QueueName { get; init; }
}
