using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PartnerIntegration.BFF.Core.Models;
using PartnerIntegration.BFF.Core.Services;

namespace PartnerIntegration.BFF.Api.Controllers;

[ApiController]
[Route("api/v1/partner")]
[Authorize]
public class PartnerTransactionsController(ITransactionService transactionService) : ControllerBase
{
    [HttpPost("transactions")]
    [ProducesResponseType(typeof(TransactionAcceptedResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTransaction(
        [FromBody] PartnerTransactionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await transactionService.ProcessTransactionAsync(request, cancellationToken);

        if (!result.IsSuccess)
        {
            return result.ErrorType switch
            {
                TransactionErrorType.PartnerNotVerified => Problem(statusCode: StatusCodes.Status403Forbidden, title: "Partner Verification Failed", detail: result.ErrorMessage),
                _ => Problem(statusCode: StatusCodes.Status500InternalServerError)
            };
        }

        return Accepted(new TransactionAcceptedResponse("Transaction accepted and queued for processing.", request.TransactionReference, DateTimeOffset.UtcNow));
    }
}
