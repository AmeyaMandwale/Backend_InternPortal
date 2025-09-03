using Xunit; // [Fact], Assert
using Moq; // Mock<>
using Moq.Protected; // Protected() for HttpMessageHandler
using Microsoft.EntityFrameworkCore; // UseInMemoryDatabase
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

using Microsoft.AspNetCore.Mvc; // IActionResult, OkObjectResult

using Backend_InternPortal.Controllers; // ActivitiesController
using InternConnect_Backend.Data; // ApplicationDbContext
using InternConnect_Backend.Models; // Activity, GenerateActivitiesRequest, Profile


namespace Backend_ProjectTests.Controllers
{
    public class ActivitiesControllerTest
    {
        private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly ApplicationDbContext _dbContext;

        public ActivitiesControllerTest()
        {
            // In-memory database
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "TestDb")
                .Options;
            _dbContext = new ApplicationDbContext(options);

            _mockHttpClientFactory = new Mock<IHttpClientFactory>();
            _mockConfig = new Mock<IConfiguration>();
            _mockConfig.Setup(c => c["Gemini:ApiKey"]).Returns("FAKE_KEY");
        }

        [Fact]
        public async Task GenerateActivities_ProfileNotFound_ReturnsNotFound()
        {
            // Arrange
            var controller = new ActivitiesController(_dbContext, _mockHttpClientFactory.Object, _mockConfig.Object);
            var request = new GenerateActivitiesRequest { UserId = 999, Count = 3 };

            // Act
            var result = await controller.GenerateActivities(request);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal("Profile not found for given UserId", notFoundResult.Value);
        }

        [Fact]
        public async Task GetActivitiesByUserId_NoActivities_ReturnsNotFound()
        {
            var controller = new ActivitiesController(_dbContext, _mockHttpClientFactory.Object, _mockConfig.Object);

            var result = await controller.GetActivitiesByUserId(1);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("No Activity found", notFoundResult.Value.ToString());
        }

        [Fact]
        public async Task GetActivitiesByUserId_WithActivities_ReturnsOk()
        {
            // Arrange
            _dbContext.Activities.AddRange(new List<Activity>
            {
                new Activity { ActivityId = 1, UserId = 1, StepNumber = 1, Heading = "Test", Status = "Pending" },
                new Activity { ActivityId = 2, UserId = 1, StepNumber = 2, Heading = "Test 2", Status = "Pending" }
            });
            await _dbContext.SaveChangesAsync();

            var controller = new ActivitiesController(_dbContext, _mockHttpClientFactory.Object, _mockConfig.Object);

            // Act
            var result = await controller.GetActivitiesByUserId(1);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var activities = Assert.IsAssignableFrom<List<Activity>>(okResult.Value);
            Assert.Equal(2, activities.Count);
        }

        [Fact]
        public async Task GenerateActivities_GeminiApiFails_ReturnsStatusCode()
        {
            // Arrange profile
            var profile = new Profile { UserId = 1, CareerGoal = "Software Engineer" };
            _dbContext.Profiles.Add(profile);
            await _dbContext.SaveChangesAsync();

            var mockHttp = new Mock<HttpMessageHandler>();
            mockHttp
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<System.Threading.CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent("Error")
                });

            var client = new HttpClient(mockHttp.Object);
            _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

            var controller = new ActivitiesController(_dbContext, _mockHttpClientFactory.Object, _mockConfig.Object);

            var request = new GenerateActivitiesRequest { UserId = 1, Count = 2 };

            // Act
            var result = await controller.GenerateActivities(request);

            // Assert
            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(400, statusResult.StatusCode);
            Assert.Contains("Failed to generate activities from Gemini API", statusResult.Value.ToString());
        }

        [Fact]
        public async Task GenerateActivities_ValidResponse_SavesActivities()
        {
            // Arrange profile
            var profile = new Profile { UserId = 1, CareerGoal = "Software Engineer" };
            _dbContext.Profiles.Add(profile);
            await _dbContext.SaveChangesAsync();

            var fakeActivities = new List<Activity>
            {
                new Activity { ActivityId = 0, StepNumber = 1, Heading = "Activity 1" },
                new Activity { ActivityId = 0, StepNumber = 2, Heading = "Activity 2" }
            };
            string aiResponse = JsonConvert.SerializeObject(fakeActivities);

            var mockHttp = new Mock<HttpMessageHandler>();
            mockHttp
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<System.Threading.CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(@"{ ""candidates"": [ { ""content"": { ""parts"": [ { ""text"": """ + aiResponse + @""" } ] } } ] }")
                });

            var client = new HttpClient(mockHttp.Object);
            _mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

            var controller = new ActivitiesController(_dbContext, _mockHttpClientFactory.Object, _mockConfig.Object);

            var request = new GenerateActivitiesRequest { UserId = 1, Count = 2 };

            // Act
            var result = await controller.GenerateActivities(request);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var activities = Assert.IsAssignableFrom<List<Activity>>(okResult.Value);
            Assert.Equal(2, activities.Count);
            Assert.All(activities, a => Assert.Equal(1, a.UserId));
        }
    }
}
