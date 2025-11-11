using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Tracker.API.Controllers;
using Xunit;

namespace Tracker.Tests.Controllers;

public class HealthControllerTests
{
    private readonly HealthController _controller;
    private readonly Mock<ILogger<HealthController>> _loggerMock;

    public HealthControllerTests()
    {
        _loggerMock = new Mock<ILogger<HealthController>>();
        _controller = new HealthController(_loggerMock.Object);
    }

    [Fact]
    public void Get_ReturnsOkResult()
    {
        // Act
        var result = _controller.Get();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        Assert.NotNull(response);
        
        // Use reflection to safely check the Status property
        var statusProperty = response.GetType().GetProperty("Status");
        Assert.NotNull(statusProperty);
        var statusValue = statusProperty.GetValue(response)?.ToString();
        Assert.Equal("Healthy", statusValue);
    }
}
