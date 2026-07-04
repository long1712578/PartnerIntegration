using FluentValidation;

namespace PartnerIntegration.BFF.Api.Filters;

public class ValidationFilter<T>(IValidator<T> validator) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var argument = context.Arguments.FirstOrDefault(a => a is T);
        if (argument is null)
        {
            return Results.BadRequest("Invalid payload format.");
        }

        var validationResult = await validator.ValidateAsync((T)argument);
        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        return await next(context);
    }
}