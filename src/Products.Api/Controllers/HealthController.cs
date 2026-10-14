using Microsoft.AspNetCore.Mvc;

namespace Products.Api.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
public class HealthController : ControllerBase
{
    [HttpGet("/health")]
    public IActionResult Get() => Ok(new { status = "ok" });
}
