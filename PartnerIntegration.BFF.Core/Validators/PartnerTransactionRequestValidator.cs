using FluentValidation;
using PartnerIntegration.BFF.Core.Models;

namespace PartnerIntegration.BFF.Core.Validators;

public class PartnerTransactionRequestValidator : AbstractValidator<PartnerTransactionRequest>
{
    private static readonly HashSet<string> ValidCurrencies = new(["USD", "EUR", "VND", "JPY"], StringComparer.OrdinalIgnoreCase);

    public PartnerTransactionRequestValidator()
    {
        RuleFor(x => x.PartnerId)
            .NotEmpty()
            .WithMessage("PartnerId is required.");

        RuleFor(x => x.TransactionReference)
            .NotEmpty()
            .WithMessage("TransactionReference is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0)
            .WithMessage("Amount must be greater than zero.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Must(BeAValidCurrency)
            .WithMessage("Currency is not valid.");

        RuleFor(x => x.Timestamp)
            .NotEmpty()
            .WithMessage("Timestamp is required.");
    }

    private static bool BeAValidCurrency(string currency) => ValidCurrencies.Contains(currency);
}
