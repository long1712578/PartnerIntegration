using PartnerIntegration.BFF.Core.Models;
using PartnerIntegration.BFF.Core.Validators;

namespace PartnerIntegration.BFF.Tests.Validators;

public class PartnerTransactionRequestValidatorTests
{
    private readonly PartnerTransactionRequestValidator _validator;

    public PartnerTransactionRequestValidatorTests()
    {
        _validator = new PartnerTransactionRequestValidator();
    }

    [Fact]
    public async Task Validate_WithValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var request = new PartnerTransactionRequest(
            PartnerId: "P-1000",
            TransactionReference: "TXN-99823",
            Amount: 100.50m,
            Currency: "USD",
            Timestamp: DateTimeOffset.UtcNow);

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_WithEmptyPartnerId_ShouldReturnError()
    {
        // Arrange
        var request = new PartnerTransactionRequest(
            PartnerId: "",
            TransactionReference: "TXN-001",
            Amount: 100.50m,
            Currency: "USD",
            Timestamp: DateTimeOffset.UtcNow);

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "PartnerId");
    }

    [Fact]
    public async Task Validate_WithInvalidCurrency_ShouldReturnError()
    {
        // Arrange
        var request = new PartnerTransactionRequest(
            PartnerId: "P-1001",
            TransactionReference: "TXN-001",
            Amount: 100.50m,
            Currency: "XYZ",
            Timestamp: DateTimeOffset.UtcNow);

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Currency");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-100)]
    public async Task Validate_WithInvalidAmount_ShouldReturnError(decimal amount)
    {
        // Arrange
        var request = new PartnerTransactionRequest(
            PartnerId: "P-1001",
            TransactionReference: "TXN-001",
            Amount: amount,
            Currency: "USD",
            Timestamp: DateTimeOffset.UtcNow);

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Amount");
    }

    [Theory]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("VND")]
    [InlineData("JPY")]
    public async Task Validate_WithValidCurrencies_ShouldReturnSuccess(string currency)
    {
        // Arrange
        var request = new PartnerTransactionRequest(
            PartnerId: "P-1001",
            TransactionReference: "TXN-001",
            Amount: 100.50m,
            Currency: currency,
            Timestamp: DateTimeOffset.UtcNow);

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Validate_WithEmptyTransactionReference_ShouldReturnError()
    {
        // Arrange
        var request = new PartnerTransactionRequest(
            PartnerId: "P-1001",
            TransactionReference: "",
            Amount: 100.50m,
            Currency: "USD",
            Timestamp: DateTimeOffset.UtcNow);

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "TransactionReference");
    }

    [Fact]
    public async Task Validate_WithEmptyCurrency_ShouldReturnError()
    {
        // Arrange
        var request = new PartnerTransactionRequest(
            PartnerId: "P-1001",
            TransactionReference: "TXN-001",
            Amount: 100.50m,
            Currency: "",
            Timestamp: DateTimeOffset.UtcNow);

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Currency");
    }

    [Fact]
    public async Task Validate_WithMultipleErrors_ShouldReturnAllErrors()
    {
        // Arrange
        var request = new PartnerTransactionRequest(
            PartnerId: "",
            TransactionReference: "",
            Amount: -1,
            Currency: "INVALID",
            Timestamp: DateTimeOffset.UtcNow);

        // Act
        var result = await _validator.ValidateAsync(request);

        // Assert
        Assert.False(result.IsValid);
        Assert.True(result.Errors.Count >= 4, "Should have errors for PartnerId, TransactionReference, Amount, and Currency");
    }
}
