using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PartnerIntegration.BFF.Core.Services;
using PartnerIntegration.BFF.Core.Validators;

namespace PartnerIntegration.BFF.Core.Extensions;

public static class CoreServiceCollectionExtensions
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<PartnerTransactionRequestValidator>(includeInternalTypes: true);
        services.AddScoped<ITransactionService, TransactionService>();

        return services;
    }
}
