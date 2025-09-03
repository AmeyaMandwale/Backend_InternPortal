using Xunit;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;
using Backend_Project.Controllers;
using InternConnect_Backend.Data;
using InternConnect_Backend.Models;
using InternConnect_Backend.Services;

namespace Backend_InternPortal.Tests
{
    public class OpportunityControllerTest
    {
        private ApplicationDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // fresh DB each test
                .Options;

            return new ApplicationDbContext(options);
        }

        [Fact]
        public async Task GetOpportunitiesByUser_ReturnsCorrectOpportunities()
        {
            // Arrange
            var db = GetInMemoryDbContext();
            db.Opportunities.AddRange(
                new Opportunity { OpportunityId = 1, UserId = 1, Position = "Intern", Company = "ABC" },
                new Opportunity { OpportunityId = 2, UserId = 2, Position = "Dev", Company = "XYZ" }
            );
            await db.SaveChangesAsync();

            var mockSerpApi = new Mock<SerpApiService>(null!);
            var controller = new OpportunityController(db, mockSerpApi.Object);

            // Act
            var result = await controller.GetOpportunitiesByUser(1);

            // Assert
            var okResult = Assert.IsType<ActionResult<IEnumerable<Opportunity>>>(result);
            var list = Assert.IsAssignableFrom<IEnumerable<Opportunity>>(okResult.Value);
            Assert.Single(list);
            Assert.Equal("Intern", list.First().Position);
        }

        [Fact]
        public async Task UpdateOpportunity_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            var db = GetInMemoryDbContext();
            var opp = new Opportunity { OpportunityId = 1, UserId = 1, Position = "Intern", Company = "ABC", Status = "None" };
            db.Opportunities.Add(opp);
            await db.SaveChangesAsync();

            var mockSerpApi = new Mock<SerpApiService>(null!);
            var controller = new OpportunityController(db, mockSerpApi.Object);

            var updated = new Opportunity { OpportunityId = 1, UserId = 1, Position = "Intern", Company = "ABC", Status = "Approved", IsApplied = true };

            // Act
            var result = await controller.UpdateOpportunity(1, updated);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value as dynamic;
            Assert.Equal("Opportunity updated successfully.", response.message);
            Assert.True(((Opportunity)response.data).IsApplied);
            Assert.Equal("Approved", ((Opportunity)response.data).Status);
        }

        [Fact]
        public async Task UpdateOpportunity_ReturnsBadRequest_WhenIdMismatch()
        {
            var db = GetInMemoryDbContext();
            var mockSerpApi = new Mock<SerpApiService>(null!);
            var controller = new OpportunityController(db, mockSerpApi.Object);

            var updated = new Opportunity { OpportunityId = 2, UserId = 1, Position = "Intern", Company = "ABC" };

            var result = await controller.UpdateOpportunity(1, updated);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("ID mismatch", badRequest.Value.ToString());
        }

        [Fact]
        public async Task UpdateOpportunity_ReturnsNotFound_WhenOpportunityDoesNotExist()
        {
            var db = GetInMemoryDbContext();
            var mockSerpApi = new Mock<SerpApiService>(null!);
            var controller = new OpportunityController(db, mockSerpApi.Object);

            var updated = new Opportunity { OpportunityId = 1, UserId = 1, Position = "Intern", Company = "ABC" };

            var result = await controller.UpdateOpportunity(1, updated);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task FetchByCareerGoal_ReturnsFilteredOpportunities()
        {
            // Arrange
            var db = GetInMemoryDbContext();
            db.Profiles.Add(new InternConnect_Backend.Models.Profile { ProfileId = 1, UserId = 1, CareerGoal = "Developer" });

            var opps = new List<Opportunity>
            {
                new Opportunity { OpportunityId = 1, UserId = 1, Position = "Software Developer Intern", Company = "ABC", PostedDate = DateTime.UtcNow },
                new Opportunity { OpportunityId = 2, UserId = 1, Position = "Marketing Intern", Company = "XYZ", PostedDate = DateTime.UtcNow }
            };

            var mockSerpApi = new Mock<SerpApiService>(null!);
            mockSerpApi.Setup(s => s.FetchOpportunitiesAsync("Developer", "India", 1))
                       .ReturnsAsync(opps);

            db.SaveChanges();

            var controller = new OpportunityController(db, mockSerpApi.Object);

            // Act
            var result = await controller.FetchByCareerGoal(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var finalList = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
            Assert.Single(finalList); // only Developer-related
        }
    }
}
