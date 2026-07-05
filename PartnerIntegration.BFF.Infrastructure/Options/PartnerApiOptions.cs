namespace PartnerIntegration.BFF.Infrastructure.Options;

public sealed class PartnerApiOptions
{
    public const string SectionName = "PartnerApi";

    public required string BaseAddress { get; init; }
    public int TimeoutSeconds { get; init; } = 30;
}
