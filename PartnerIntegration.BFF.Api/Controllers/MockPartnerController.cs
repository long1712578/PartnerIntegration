using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PartnerIntegration.BFF.Api.Controllers;

[ApiController]
[Route("internal")]
[AllowAnonymous]
public class MockPartnerController : ControllerBase
{
    [HttpGet("mock-partner/{id}")]
    public IActionResult VerifyPartner(string id)
    {
        if (Random.Shared.Next(1, 101) <= 30)
            throw new TimeoutException("Simulated partner API timeout.");

        return Ok(new { PartnerId = id, Status = "Active" });
    }
}
