using FluentValidation;
using Microsoft.AspNetCore.Mvc;
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

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/api/v1/partner/transactions", async ([FromBody] PartnerTransactionRequest request, [FromServices] IPartnerVerificationClient partnerClient,
                                                   [FromServices] ITransactionMessagePublisher messagePublisher, CancellationToken cancellationToken) =>
{
    // Validate Payload

    // Verify Partner qua External API
    var isPartnerValid = await partnerClient.VerifyPartnerAsync(request.PartnerId, cancellationToken);

    if (!isPartnerValid)
    {
        return Results.Problem(statusCode: 403, title: "Partner Verification Failed", detail: "The provided PartnerId is invalid or inactive.");
    }

    // Push into Queue
    await messagePublisher.PublishTransactionAsync(request, cancellationToken);

    return Results.Accepted(value: new { Message = "Transaction accepted and queued for processing." });
})
.AddEndpointFilter<ValidationFilter<PartnerTransactionRequest>>(); // Gắn Validation Filter vào endpoint


// Mock "Partner Verification API" internal
app.MapGet("/internal/mock-partner/{id}", (string id) =>
{
    // Random error 30% to test Retry logic
    var randomValue = Random.Shared.Next(1, 101);

    if (randomValue <= 30)
    {
        throw new TimeoutException("Simulated partner API timeout.");
    }

    // 70% success
    return Results.Ok(new { PartnerId = id, Status = "Active" });
});

app.Run();
