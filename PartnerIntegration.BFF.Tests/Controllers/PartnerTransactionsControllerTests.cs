using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PartnerIntegration.BFF.Api.Controllers;
using PartnerIntegration.BFF.Core.Models;
using PartnerIntegration.BFF.Core.Services;

namespace PartnerIntegration.BFF.Tests.Controllers;

public class PartnerTransactionsControllerTests
{
    private readonly Mock<ITransactionService> _serviceMock;
    private readonly PartnerTransactionsController _controller;

    public PartnerTransactionsControllerTests()
    {
        _serviceMock = new Mock<ITransactionService>();
        _controller = new PartnerTransactionsController(_serviceMock.Object);
    }

    [Fact]
    public async Task CreateTransaction_WhenSuccess_Returns202Accepted()
    {
        // Arrange
        var request = CreateValidRequest();
        _serviceMock
            .Setup(x => x.ProcessTransactionAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TransactionResult.Accepted(request.TransactionReference));

        // Act
        var result = await _controller.CreateTransaction(request, CancellationToken.None);

        // Assert
        var acceptedResult = result.Should().BeOfType<AcceptedResult>().Subject;
        var response = acceptedResult.Value.Should().BeOfType<TransactionAcceptedResponse>().Subject;
        response.TransactionReference.Should().Be(request.TransactionReference);
        response.Message.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task CreateTransaction_WhenPartnerNotVerified_Returns403()
    {
        // Arrange
        var request = CreateValidRequest();
        _serviceMock
            .Setup(x => x.ProcessTransactionAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TransactionResult.PartnerNotVerified());

        // Act
        var result = await _controller.CreateTransaction(request, CancellationToken.None);

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task CreateTransaction_WhenSuccess_ResponseIncludesAcceptedAt()
    {
        // Arrange
        var request = CreateValidRequest();
        _serviceMock
            .Setup(x => x.ProcessTransactionAsync(request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(TransactionResult.Accepted(request.TransactionReference));

        // Act
        var before = DateTimeOffset.UtcNow;
        var result = await _controller.CreateTransaction(request, CancellationToken.None);
        var after = DateTimeOffset.UtcNow;

        // Assert
        var acceptedResult = result.Should().BeOfType<AcceptedResult>().Subject;
        var response = acceptedResult.Value.Should().BeOfType<TransactionAcceptedResponse>().Subject;
        response.AcceptedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    private static PartnerTransactionRequest CreateValidRequest() => new(
        PartnerId: "P-1001",
        TransactionReference: "TXN-99823",
        Amount: 250.00m,
        Currency: "USD",
        Timestamp: DateTimeOffset.UtcNow);
}
