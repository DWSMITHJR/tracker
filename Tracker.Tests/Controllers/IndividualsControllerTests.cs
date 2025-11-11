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

public class IndividualsControllerTests : IDisposable
{
    private readonly DbContextOptions<ApplicationDbContext> _options;
    private readonly ApplicationDbContext _context;
    private readonly Mock<ILogger<IndividualsController>> _mockLogger;
    private readonly IndividualsController _controller;
    private readonly Guid _testUserId = Guid.NewGuid();
    private readonly Guid _testOrganizationId = Guid.NewGuid();
    private readonly Guid _testIndividualId = Guid.NewGuid();

    public IndividualsControllerTests()
    {
        // Use in-memory database for testing
        _options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: "IndividualsTestDb_" + Guid.NewGuid())
            .Options;

        _context = new ApplicationDbContext(_options);
        _mockLogger = new Mock<ILogger<IndividualsController>>();
        _controller = new IndividualsController(_context, _mockLogger.Object);

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

        // Add test individual
        var individual = new Individual
        {
            Id = _testIndividualId,
            FirstName = "John",
            LastName = "Doe",
            DateOfBirth = new DateTime(1990, 1, 1),
            Gender = "Male",
            OrganizationId = _testOrganizationId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Individuals.Add(individual);

        _context.SaveChanges();
    }

    [Fact]
    public async Task GetIndividualsByOrganization_WithValidOrganization_ReturnsIndividuals()
    {
        try
        {
            // Act
            var result = await _controller.GetIndividualsByOrganization(_testOrganizationId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var individuals = Assert.IsAssignableFrom<IEnumerable<Individual>>(okResult.Value);
            var individualList = individuals.ToList();
            
            Assert.NotEmpty(individualList);
            Assert.Contains(individualList, i => i.FirstName == "John");
        }
        catch (Exception ex)
        {
            _mockLogger.Object.LogError(ex, "Error in GetIndividualsByOrganization test");
            throw;
        }
    }

    [Fact]
    public async Task GetIndividual_WithValidId_ReturnsIndividual()
    {
        try
        {
            // Act
            var result = await _controller.GetIndividual(_testIndividualId);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var individual = Assert.IsType<Individual>(okResult.Value);
            
            Assert.Equal(_testIndividualId, individual.Id);
            Assert.Equal("John", individual.FirstName);
            Assert.Equal(_testOrganizationId, individual.OrganizationId);
        }
        catch (Exception ex)
        {
            _mockLogger.Object.LogError(ex, "Error in GetIndividual test");
            throw;
        }
    }

    [Fact]
    public async Task CreateIndividual_WithValidData_ReturnsCreatedIndividual()
    {
        try
        {
            // Arrange
            var newIndividual = new Individual
            {
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane.smith@example.com",
                DateOfBirth = new DateTime(1995, 5, 15),
                Gender = "Female",
                OrganizationId = _testOrganizationId,
                IsActive = true
            };

            // Act
            var result = await _controller.CreateIndividual(newIndividual);

            // Assert
            var createdAtResult = Assert.IsType<CreatedAtActionResult>(result);
            var individual = Assert.IsType<Individual>(createdAtResult.Value);
            
            Assert.Equal("Jane", individual.FirstName);
            Assert.Equal("Smith", individual.LastName);
            Assert.Equal(_testOrganizationId, individual.OrganizationId);
            Assert.True(individual.IsActive);
            Assert.NotEqual(Guid.Empty, individual.Id);
        }
        catch (Exception ex)
        {
            _mockLogger.Object.LogError(ex, "Error in CreateIndividual test");
            throw;
        }
    }

    [Fact]
    public async Task UpdateIndividual_WithValidData_ReturnsUpdatedIndividual()
    {
        try
        {
            // Arrange
            var existingIndividual = await _context.Individuals.FindAsync(_testIndividualId);
            Assert.NotNull(existingIndividual);
            
            var updatedIndividual = new Individual
            {
                Id = _testIndividualId,
                FirstName = "John Updated",
                LastName = existingIndividual.LastName,
                Email = existingIndividual.Email,
                DateOfBirth = existingIndividual.DateOfBirth,
                Gender = existingIndividual.Gender,
                OrganizationId = _testOrganizationId,
                IsActive = true
            };

            // Act
            var result = await _controller.UpdateIndividual(_testIndividualId, updatedIndividual);

            // Assert
            // The controller returns NoContentResult on successful update
            Assert.IsType<NoContentResult>(result);

            // Verify the individual was updated in the database
            var updated = await _context.Individuals.FindAsync(_testIndividualId);
            Assert.NotNull(updated);
            Assert.Equal("John Updated", updated.FirstName);
            Assert.Equal(_testOrganizationId, updated.OrganizationId);
            Assert.True(updated.IsActive);
        }
        catch (Exception ex)
        {
            _mockLogger.Object.LogError(ex, "Error in UpdateIndividual test");
            throw;
        }
    }

    [Fact]
    public async Task DeleteIndividual_WithValidId_ReturnsNoContent()
    {
        try
        {
            // Act
            var result = await _controller.DeleteIndividual(_testIndividualId);

            // Assert
            Assert.IsType<NoContentResult>(result);

            // Verify the individual is marked as inactive
            var individual = await _context.Individuals.FindAsync(_testIndividualId);
            Assert.NotNull(individual);
            Assert.False(individual.IsActive);
        }
        catch (Exception ex)
        {
            _mockLogger.Object.LogError(ex, "Error in DeleteIndividual test");
            throw;
        }
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
