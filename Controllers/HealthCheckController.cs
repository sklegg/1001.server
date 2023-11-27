using Microsoft.AspNetCore.Mvc;
using Server1001.Services;
using Server1001.Shared;

namespace Server1001.Controllers;

[ApiController]
[Route("health")]
public class HealthCheckController : ControllerBase
{
    private ILogger<HealthCheckController> _logger;
    private IDynamoRepository _repository;

    public HealthCheckController(ILogger<HealthCheckController> logger, IDynamoRepository repository)
    {
        _logger = logger;
        _repository = repository;
    }

    [HttpGet("ping")]
    public IActionResult PingDong()
    {
        return Ok("dong");
    }

    [HttpGet("db")]
    public async Task<IActionResult> CheckDatabase() {
        var result = await _repository.HealthCheck();
        _logger.LogCritical(Events.DatabaseFailure, "Database health check failed!");
        return Ok(result.ToString());
    }    
}