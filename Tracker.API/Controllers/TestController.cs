using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tracker.Infrastructure.Data;

namespace Tracker.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TestController> _logger;

        public TestController(ApplicationDbContext context, ILogger<TestController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("db")]
        public async Task<IActionResult> TestDatabase()
        {
            try
            {
                _logger.LogInformation("Testing database connection...");
                
                // Test connection by executing a simple query
                var canConnect = await _context.Database.CanConnectAsync();
                
                if (canConnect)
                {
                    _logger.LogInformation("Successfully connected to the database!");
                    return Ok(new { 
                        Success = true, 
                        Message = "Successfully connected to the database!" 
                    });
                }
                
                return StatusCode(500, new { 
                    Success = false, 
                    Message = "Could not connect to the database" 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing database connection");
                return StatusCode(500, new { 
                    Success = false, 
                    Message = "Error testing database connection",
                    Error = ex.Message,
                    StackTrace = ex.StackTrace
                });
            }
        }
    }
}
