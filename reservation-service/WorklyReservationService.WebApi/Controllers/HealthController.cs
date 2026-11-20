using Microsoft.AspNetCore.Mvc;

namespace WorklyReservationService.WebApi.Controllers;

[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { status = "healthy", service = "reservation-service" });
    }
}
