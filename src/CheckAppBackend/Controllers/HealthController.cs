using CheckAppBackend.Domain;
using Microsoft.AspNetCore.Mvc;

namespace CheckAppBackend.Controllers;

[ApiController]
[Route("api/v1/health")]
public class HealthController : ControllerBase
{
    private readonly IServiceStatus _status;

    public HealthController(IServiceStatus status)
    {
        _status = status;
    }

    [HttpGet]
    public ActionResult<HealthResponse> GetHealth()
    {
        return Ok(new HealthResponse(_status.CurrentStatus(), ServiceInfo.Name));
    }
}
