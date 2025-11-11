using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Tracker.API.Controllers;
using Tracker.Infrastructure.Data;
using Tracker.Infrastructure.Models;
using Tracker.Tests.Helpers;
using Xunit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Tracker.Tests.Controllers;

public class IncidentsControllerTests : IDisposable
{
    private readonly DbContextOptions<ApplicationDbContext> _options;
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<IncidentsController>> _mockLogger;
    private readonly IncidentsController _controller;
    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly Guid _testOrganizationId = Guid.NewGuid();

    public IncidentsControllerTests()
    {
        // Use in-memory database for testing
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "TestDb_" + Guid.NewGuid())
            .Options;

        _context = new ApplicationDbContext(_options);
        _mockLogger = new Mock<ILogger<IncidentsController>>();
        _controller = new IncidentsController(_context, _mockLogger.Object);

        // Set up test user with TestHelper
        TestHelper.SetupTestUser(
            _controller, 
            _testUserId, 
            "testuser@example.com", 
            "Admin", 
            _testOrganizationId);

        // Seed test data
        SeedTestData();
    }

    private void SeedTestData()
    {
        // Add test organization
        var organization = new Organization
        {
            Id = _testOrganizationId,
            Name = "Test Organization",
            IsActive = true
        };
        _context.Organizations.Add(organization);

        // Set up test individual
        var individual = new Individual
        {
            Id = _testUserId,
            FirstName = "Test",
            LastName = "User",
            Email = "testuser@example.com"
        };
        _context.Individuals.Add(individual);

        // Add test incident
        var incident = new Incident
        {
            Id = Guid.NewGuid(),
            Title = "Test Incident",
            Description = "Test Description",
            Status = "Open",
            Severity = "Medium",
            OrganizationId = _testOrganizationId,
            ReportedById = _testUserId, // This should be the ID of an Individual, not an ApplicationUser
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Incidents.Add(incident);

        _context.SaveChanges();
    }

    [Fact]
    public async Task GetIncidentsByOrganization_WithValidOrganization_ReturnsIncidents()
    {
        try
        {
            // Act
            var result = await _controller.GetIncidentsByOrganization(_testOrganizationId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var incidents = Assert.IsAssignableFrom<IEnumerable<Incident>>(okResult.Value);
            var incidentList = incidents.ToList();
            Assert.NotEmpty(incidentList);
            Assert.Contains(incidentList, i => i.Title == "Test Incident");
        }
        catch (Exception ex)
        {
            // Log the exception for debugging
            _mockLogger.Object.LogError(ex, "Error in GetIncidentsByOrganization test");
            throw;
        }
    }

    [Fact]
    public async Task GetIncident_WithValidId_ReturnsIncident()
    {
        try
        {
            // Arrange
            var incident = _context.Incidents.First();
            var incidentId = incident.Id;

            // Act
            var result = await _controller.GetIncident(incidentId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedIncident = Assert.IsType<Incident>(okResult.Value);
            Assert.Equal(incidentId, returnedIncident.Id);
            Assert.Equal(incident.Title, returnedIncident.Title);
        }
        catch (Exception ex)
        {
            _mockLogger.Object.LogError(ex, "Error in GetIncident test");
            throw;
        }
    }

    [Fact]
    public async Task CreateIncident_WithValidData_ReturnsCreatedIncident()
    {
        try
        {
            // Arrange
            var newIncident = new Incident
            {
                Title = "New Incident",
                Description = "New Description",
                Status = "Open",
                Severity = "Low",
                OrganizationId = _testOrganizationId,
                IsActive = true
            };

            // Act
            var result = await _controller.CreateIncident(newIncident);

            // Assert
            var createdAtResult = Assert.IsType<CreatedAtActionResult>(result);
            var incident = Assert.IsType<Incident>(createdAtResult.Value);
            
            // Verify the incident was created with the correct values
            Assert.Equal("New Incident", incident.Title);
            Assert.Equal(_testOrganizationId, incident.OrganizationId);
            Assert.True(incident.IsActive);
            
            // The controller should set the ReportedById to the current user's ID
            Assert.Equal(_testUserId, incident.ReportedById);
            Assert.NotEqual(Guid.Empty, incident.Id);
        }
        catch (Exception ex)
        {
            _mockLogger.Object.LogError(ex, "Error in CreateIncident test");
            throw;
        }
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
