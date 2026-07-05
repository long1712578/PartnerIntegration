namespace PartnerIntegration.BFF.Core.Models;

/// <summary>
/// Used to communicate results from the service layer to the controller
/// </summary>
public sealed class TransactionResult
{
    public bool IsSuccess { get; private init; }
    public string? ErrorMessage { get; private init; }
    public TransactionErrorType? ErrorType { get; private init; }
    public string? TransactionReference { get; private init; }

    public static TransactionResult Accepted(string transactionReference) => new()
    {
        IsSuccess = true,
        TransactionReference = transactionReference
    };

    public static TransactionResult PartnerNotVerified() => new()
    {
        IsSuccess = false,
        ErrorMessage = "The provided PartnerId is invalid or inactive.",
        ErrorType = TransactionErrorType.PartnerNotVerified
    };
}

public enum TransactionErrorType
{
    PartnerNotVerified
}
