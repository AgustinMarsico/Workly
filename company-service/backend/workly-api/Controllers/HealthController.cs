using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace workly_api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("health")]
public class HealthController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get()
    {
        return Ok(new { status = "healthy" });
    }
}
