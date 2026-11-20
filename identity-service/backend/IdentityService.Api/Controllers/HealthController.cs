using Microsoft.AspNetCore.Mvc;

namespace workly_api.Controllers;

[ApiController]
public sealed class HealthController : ControllerBase
{
    [HttpGet("health")]
    [HttpGet("api/health")]
    public IActionResult Get() => Ok(new { status = "healthy", service = "identity-service" });
}
