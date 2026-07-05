using PartnerIntegration.BFF.Api.Extensions;
using PartnerIntegration.BFF.Api.Filters;
using PartnerIntegration.BFF.Core.Extensions;
using PartnerIntegration.BFF.Core.Interfaces;
using PartnerIntegration.BFF.Core.Models;
using PartnerIntegration.BFF.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddCoreServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

var app = builder.Build();

app.UseGlobalExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.MapPost("/api/v1/partner/transactions", async (
    PartnerTransactionRequest request,
    IPartnerVerificationClient partnerClient,
    ITransactionMessagePublisher messagePublisher,
    CancellationToken cancellationToken) =>
{
    var isPartnerValid = await partnerClient.VerifyPartnerAsync(request.PartnerId, cancellationToken);

    if (!isPartnerValid)
    {
        return Results.Problem(statusCode: 403, title: "Partner Verification Failed",
            detail: "The provided PartnerId is invalid or inactive.");
    }

    await messagePublisher.PublishTransactionAsync(request, cancellationToken);

    return Results.Accepted(value: new { Message = "Transaction accepted and queued for processing." });
})
.AddEndpointFilter<ValidationFilter<PartnerTransactionRequest>>();


// Mock "Partner Verification API" — simulates 30% timeout / 70% success per requirements
app.MapGet("/internal/mock-partner/{id}", (string id) =>
{
    if (Random.Shared.Next(1, 101) <= 30)
        throw new TimeoutException("Simulated partner API timeout.");

    return Results.Ok(new { PartnerId = id, Status = "Active" });
});

app.Run();
