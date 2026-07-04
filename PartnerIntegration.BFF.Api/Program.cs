using FluentValidation;
using PartnerIntegration.BFF.Core.Extensions;
using PartnerIntegration.BFF.Core.Interfaces;
using PartnerIntegration.BFF.Core.Models;
using PartnerIntegration.BFF.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddCoreServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/api/transactions", async (
    PartnerTransactionRequest request,
    IValidator<PartnerTransactionRequest> validator,
    IPartnerVerificationClient verificationClient,
    ITransactionMessagePublisher messagePublisher,
    ILogger<Program> logger,
    CancellationToken cancellationToken) =>
{
    try
    {
        var validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            logger.LogWarning("Transaction validation failed for PartnerId: {PartnerId}", request.PartnerId);
            return Results.BadRequest(new { errors = validationResult.Errors.Select(e => e.ErrorMessage) });
        }
        logger.LogInformation("Verifying partner: {PartnerId} for transaction: {TransactionReference}", request.PartnerId, request.TransactionReference);
        var isPartnerValid = await verificationClient.VerifyPartnerAsync(request.PartnerId, cancellationToken);
        
        if (!isPartnerValid)
        {
            logger.LogWarning("Partner verification failed for PartnerId: {PartnerId}", request.PartnerId);
            return Results.BadRequest(new { error = "Partner verification failed" });
        }

        logger.LogInformation("Publishing transaction: {TransactionReference}", request.TransactionReference);
        await messagePublisher.PublishTransactionAsync(request, cancellationToken);
        
        logger.LogInformation("Transaction processed successfully: {TransactionReference}", request.TransactionReference);
        return Results.Accepted("", new { transactionReference = request.TransactionReference });
    }
    catch (OperationCanceledException ex)
    {
        logger.LogWarning(ex, "Operation cancelled for transaction: {TransactionReference}", request.TransactionReference);
        return Results.StatusCode(StatusCodes.Status408RequestTimeout);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unexpected error processing transaction: {TransactionReference}", request.TransactionReference);
        return Results.StatusCode(StatusCodes.Status500InternalServerError);
    }
})
.WithName("ProcessTransaction")
.WithOpenApi()
.Produces(StatusCodes.Status202Accepted)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status408RequestTimeout)
.Produces(StatusCodes.Status500InternalServerError);

app.Run();
