using Xunit;
using Microsoft.EntityFrameworkCore;
using Backend_InternPortal.Controllers;
using InternConnect_Backend.Data;
using InternConnect_Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Backend_InternPortalTests
{
    public class UserControllerTest
    {
        private ApplicationDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "UserDb_" + System.Guid.NewGuid().ToString())
                .Options;

            var context = new ApplicationDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        private IConfiguration GetFakeConfiguration()
        {
            var inMemorySettings = new Dictionary<string, string> {
                {"Jwt:Key", "ThisIsASecretKeyForJwtToken123!"}
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
        }

        [Fact]
        public async Task Register_AddsUserSuccessfully()
        {
            var context = GetDbContext();
            var config = GetFakeConfiguration();
            var controller = new UserController(context, config);

            var newUser = new User { Email = "test@example.com", Password = "password123" };

            var result = await controller.Register(newUser);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("User registered successfully", okResult.Value.ToString());
            Assert.Single(context.Users);
        }

        [Fact]
        public async Task Register_ReturnsConflict_WhenEmailAlreadyExists()
        {
            var context = GetDbContext();
            var config = GetFakeConfiguration();

            context.Users.Add(new User { Email = "duplicate@example.com", Password = "hashed" });
            await context.SaveChangesAsync();

            var controller = new UserController(context, config);

            var newUser = new User { Email = "duplicate@example.com", Password = "password123" };

            var result = await controller.Register(newUser);

            var conflictResult = Assert.IsType<ConflictObjectResult>(result);
            Assert.Contains("already registered", conflictResult.Value.ToString());
        }

        [Fact]
        public async Task Login_ReturnsToken_WhenCredentialsAreValid()
        {
            var context = GetDbContext();
            var config = GetFakeConfiguration();

            var password = "password123";
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

            var user = new User { Email = "valid@example.com", Password = hashedPassword };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var controller = new UserController(context, config);

            var loginRequest = new LoginRequest { Email = "valid@example.com", Password = password };

            var result = await controller.Login(loginRequest);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("token", okResult.Value.ToString());
        }

        [Fact]
        public async Task Login_ReturnsUnauthorized_WhenEmailDoesNotExist()
        {
            var context = GetDbContext();
            var config = GetFakeConfiguration();
            var controller = new UserController(context, config);

            var loginRequest = new LoginRequest { Email = "nouser@example.com", Password = "password123" };

            var result = await controller.Login(loginRequest);

            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Contains("Invalid email or password", unauthorizedResult.Value.ToString());
        }

        [Fact]
        public async Task Login_ReturnsUnauthorized_WhenPasswordIsWrong()
        {
            var context = GetDbContext();
            var config = GetFakeConfiguration();

            var user = new User
            {
                Email = "wrongpass@example.com",
                Password = BCrypt.Net.BCrypt.HashPassword("correctPassword")
            };
            context.Users.Add(user);
            await context.SaveChangesAsync();

            var controller = new UserController(context, config);

            var loginRequest = new LoginRequest { Email = "wrongpass@example.com", Password = "wrongPassword" };

            var result = await controller.Login(loginRequest);

            var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
            Assert.Contains("Invalid email or password", unauthorizedResult.Value.ToString());
        }
    }
}
