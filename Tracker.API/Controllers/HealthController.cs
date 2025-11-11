using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;

namespace Tracker.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly ILogger<HealthController> _logger;

        public HealthController(ILogger<HealthController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Get()
        {
            _logger.LogInformation("Health check endpoint called");
            return Ok(new { Status = "Healthy" });
        }

        [HttpGet("error")]
        public IActionResult Error()
        {
            throw new Exception("Test exception from health check");
        }

        [HttpGet("environment")]
        public IActionResult GetEnvironment()
        {
            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
            return Ok(new { Environment = environment });
        }
    }
}
