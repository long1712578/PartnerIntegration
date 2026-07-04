using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using PartnerIntegration.BFF.Core.Validators;

namespace PartnerIntegration.BFF.Core.Extensions;

public static class CoreServiceCollectionExtensions
{
    public static IServiceCollection AddCoreServices(this IServiceCollection services)
    {
        // Quét toàn bộ assembly hiện tại và tự động đăng ký tất cả các class kế thừa AbstractValidator
        services.AddValidatorsFromAssemblyContaining<PartnerTransactionRequestValidator>(includeInternalTypes: true);

        return services;
    }
}
