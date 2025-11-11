using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Tracker.Client.Pages.Incidents;
using Tracker.Client.Services;
using Tracker.Client.TestContext;
using Tracker.Shared.Models;
using Xunit;

namespace Tracker.Client.Tests.Pages.Incidents;

public class IndexTests : TestContext
{
    private readonly Mock<IIncidentService> _incidentServiceMock;
    private readonly TestNavigationManager _navigationManager;
    private readonly Mock<ILogger<Index>> _loggerMock;
    
    public IndexTests()
    {
        ConfigureTestContext();
        
        _incidentServiceMock = new Mock<IIncidentService>();
        _navigationManager = (TestNavigationManager)Services.GetRequiredService<NavigationManager>();
        _loggerMock = new Mock<ILogger<Index>>();
        
        Services.AddSingleton(_incidentServiceMock.Object);
        Services.AddSingleton(_loggerMock.Object);
        
        // Set up default mock response
        var testIncidents = new List<IncidentDto>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Test Incident 1",
                Status = "Open",
                Severity = "High",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow
            },
            new()
            {
                Id = Guid.NewGuid(),
                Title = "Test Incident 2",
                Status = "In Progress",
                Severity = "Medium",
                CreatedAt = DateTime.UtcNow.AddDays(-2),
                UpdatedAt = DateTime.UtcNow
            }
        };
        
        _incidentServiceMock.Setup(x => x.GetIncidentsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync(new PagedResult<IncidentDto>
            {
                Items = testIncidents,
                PageNumber = 1,
                PageSize = 10,
                TotalItems = 2,
                TotalPages = 1
            });
    }
    
    [Fact]
    public void Index_RendersCorrectly()
    {
        // Act
        var cut = RenderComponent<Index>();
        
        // Assert
        Assert.Contains("Incidents", cut.Markup);
        Assert.Contains("New Incident", cut.Markup);
        
        // Verify table headers
        Assert.Contains("ID", cut.Markup);
        Assert.Contains("Title", cut.Markup);
        Assert.Contains("Status", cut.Markup);
        Assert.Contains("Severity", cut.Markup);
        Assert.Contains("Created", cut.Markup);
        Assert.Contains("Updated", cut.Markup);
        
        // Verify incident data is displayed
        Assert.Contains("Test Incident 1", cut.Markup);
        Assert.Contains("Test Incident 2", cut.Markup);
    }
    
    [Fact]
    public void ClickingNewIncident_NavigatesToCreatePage()
    {
        // Arrange
        var cut = RenderComponent<Index>();
        
        // Act
        var newButton = cut.Find("button:contains('New Incident')");
        newButton.Click();
        
        // Assert
        Assert.Equal("http://localhost/incidents/new", _navigationManager.Uri);
    }
    
    [Fact]
    public void ClickingIncidentRow_NavigatesToDetailsPage()
    {
        // Arrange
        var cut = RenderComponent<Index>();
        
        // Act - Click on the first incident row
        var firstRow = cut.Find("table tbody tr");
        firstRow.Click();
        
        // Assert - Should navigate to details page with the incident ID
        var expectedId = _incidentServiceMock.Object.GetIncidentsAsync(1, 10, null).Result.Items.First().Id;
        Assert.StartsWith($"http://localhost/incidents/{expectedId}", _navigationManager.Uri);
    }
    
    [Fact]
    public void Search_FiltersIncidents()
    {
        // Arrange
        var cut = RenderComponent<Index>();
        
        // Act - Enter search query
        var searchInput = cut.Find("input[placeholder='Search incidents...']");
        searchInput.Change("Test Incident 1");
        
        // Assert - Should call service with search query
        _incidentServiceMock.Verify(x => x.GetIncidentsAsync(
            It.IsAny<int>(), 
            It.IsAny<int>(), 
            "Test Incident 1"), 
            Times.Once);
    }
    
    [Fact]
    public void Pagination_WorksCorrectly()
    {
        // Arrange - Setup mock for pagination
        var pagedResult = new PagedResult<IncidentDto>
        {
            Items = new List<IncidentDto>(),
            PageNumber = 2,
            PageSize = 10,
            TotalItems = 25,
            TotalPages = 3
        };
        
        _incidentServiceMock.Setup(x => x.GetIncidentsAsync(2, 10, null))
            .ReturnsAsync(pagedResult);
        
        var cut = RenderComponent<Index>();
        
        // Act - Click on page 2
        var page2Button = cut.Find("button:contains('2')");
        page2Button.Click();
        
        // Assert - Should call service with page 2
        _incidentServiceMock.Verify(x => x.GetIncidentsAsync(2, 10, null), Times.Once);
    }
    
    [Fact]
    public void DeleteButton_ShowsConfirmationDialog()
    {
        // Arrange
        var cut = RenderComponent<Index>();
        
        // Act - Click delete button on first incident
        var deleteButton = cut.Find("button[title='Delete']");
        deleteButton.Click();
        
        // Assert - Confirmation dialog should be visible
        Assert.True(cut.Find("h3:contains('Delete Incident')").TextContent.Contains("Delete Incident"));
        Assert.Contains("Are you sure you want to delete this incident?", cut.Markup);
    }
    
    [Fact]
    public async Task ConfirmDelete_CallsDeleteService()
    {
        // Arrange
        var incidentId = Guid.NewGuid();
        var testIncident = new IncidentDto { Id = incidentId, Title = "Test Incident" };
        
        _incidentServiceMock.Setup(x => x.GetIncidentAsync(incidentId))
            .ReturnsAsync(testIncident);
            
        _incidentServiceMock.Setup(x => x.DeleteIncidentAsync(incidentId))
            .ReturnsAsync(true);
        
        var cut = RenderComponent<Index>();
        
        // Act - Click delete button
        var deleteButton = cut.Find("button[title='Delete']");
        deleteButton.Click();
        
        // Click confirm delete button
        var confirmButton = cut.Find("button:contains('Delete')");
        await cut.InvokeAsync(() => confirmButton.Click());
        
        // Assert - Should call delete service
        _incidentServiceMock.Verify(x => x.DeleteIncidentAsync(incidentId), Times.Once);
    }
}
