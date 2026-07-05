using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using PartnerIntegration.BFF.Core.Interfaces;
using PartnerIntegration.BFF.Core.Models;
using PartnerIntegration.BFF.Core.Services;

namespace PartnerIntegration.BFF.Tests.Services;

public class TransactionServiceTests
{
    private readonly Mock<IPartnerVerificationClient> _partnerClientMock;
    private readonly Mock<ITransactionMessagePublisher> _publisherMock;
    private readonly TransactionService _service;

    public TransactionServiceTests()
    {
        _partnerClientMock = new Mock<IPartnerVerificationClient>();
        _publisherMock = new Mock<ITransactionMessagePublisher>();
        var loggerMock = new Mock<ILogger<TransactionService>>();

        _service = new TransactionService(
            _partnerClientMock.Object,
            _publisherMock.Object,
            loggerMock.Object);
    }

    [Fact]
    public async Task ProcessTransaction_WithValidPartner_ShouldAcceptAndPublish()
    {
        // Arrange
        var request = CreateValidRequest();
        _partnerClientMock
            .Setup(x => x.VerifyPartnerAsync(request.PartnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.ProcessTransactionAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.TransactionReference.Should().Be(request.TransactionReference);
        _publisherMock.Verify(
            x => x.PublishTransactionAsync(request, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessTransaction_WithInvalidPartner_ShouldReturnNotVerified()
    {
        // Arrange
        var request = CreateValidRequest();
        _partnerClientMock
            .Setup(x => x.VerifyPartnerAsync(request.PartnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _service.ProcessTransactionAsync(request, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.ErrorType.Should().Be(TransactionErrorType.PartnerNotVerified);
        result.ErrorMessage.Should().NotBeNullOrEmpty();
        _publisherMock.Verify(
            x => x.PublishTransactionAsync(It.IsAny<PartnerTransactionRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessTransaction_WhenPublishFails_ShouldPropagateException()
    {
        // Arrange
        var request = CreateValidRequest();
        _partnerClientMock
            .Setup(x => x.VerifyPartnerAsync(request.PartnerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _publisherMock
            .Setup(x => x.PublishTransactionAsync(request, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Queue unavailable"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ProcessTransactionAsync(request, CancellationToken.None));
    }

    private static PartnerTransactionRequest CreateValidRequest() => new(
        PartnerId: "P-1001",
        TransactionReference: "TXN-99823",
        Amount: 250.00m,
        Currency: "USD",
        Timestamp: DateTimeOffset.UtcNow);
}
