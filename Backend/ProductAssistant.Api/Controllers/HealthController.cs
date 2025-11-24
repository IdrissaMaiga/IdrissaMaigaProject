using Microsoft.AspNetCore.Mvc;

namespace ProductAssistant.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }

    [HttpGet("ready")]
    public IActionResult Ready()
    {
        // Add readiness checks here (database, external services, etc.)
        return Ok(new { status = "ready", timestamp = DateTime.UtcNow });
    }
}





