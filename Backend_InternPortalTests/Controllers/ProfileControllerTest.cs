using Xunit;
using Microsoft.EntityFrameworkCore;
using Backend_InternPortal.Controllers;
using InternConnect_Backend.Data;
using InternConnect_Backend.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace Backend_InternPortalTests
{
    public class ProfileControllerTest
    {
        private ApplicationDbContext GetDbContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "ProfileDb_" + System.Guid.NewGuid().ToString()) // ✅ unique DB for each test
                .Options;

            var context = new ApplicationDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        [Fact]
        public async Task GetProfiles_ReturnsAllProfiles()
        {
            // Arrange
            var context = GetDbContext();
            context.Profiles.Add(new Profile { Name = "Test User", Email = "test@example.com", Phone = "1234567890" });
            context.Profiles.Add(new Profile { Name = "Second User", Email = "second@example.com", Phone = "9876543210" });
            await context.SaveChangesAsync();

            var controller = new ProfileController(context);

            // Act
            var result = await controller.GetProfiles();

            // Assert
            var profiles = Assert.IsAssignableFrom<IEnumerable<Profile>>(result.Value);
            Assert.Equal(2, profiles.Count());
        }

        [Fact]
        public async Task GetProfileById_ReturnsProfile_WhenExists()
        {
            // Arrange
            var context = GetDbContext();
            var profile = new Profile { Name = "Test User", Email = "test@example.com", Phone = "1234567890" };
            context.Profiles.Add(profile);
            await context.SaveChangesAsync();

            var controller = new ProfileController(context);

            // Act
            var result = await controller.GetProfileById(profile.ProfileId);

            // Assert
            var returnedProfile = Assert.IsType<Profile>(result.Value);
            Assert.Equal("Test User", returnedProfile.Name);
        }

        [Fact]
        public async Task GetProfileById_ReturnsNotFound_WhenNotExists()
        {
            var context = GetDbContext();
            var controller = new ProfileController(context);

            var result = await controller.GetProfileById(999);

            Assert.IsType<NotFoundObjectResult>(result.Result);
        }

        [Fact]
        public async Task CreateProfile_AddsProfileSuccessfully()
        {
            var context = GetDbContext();
            var controller = new ProfileController(context);

            var profile = new Profile { Name = "New User", Email = "new@example.com", Phone = "1111111111" };

            var result = await controller.CreateProfile(profile);

            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            var createdProfile = Assert.IsType<Profile>(createdResult.Value);

            Assert.Equal("New User", createdProfile.Name);
            Assert.Single(context.Profiles);
        }

        [Fact]
        public async Task UpdateProfileByUserId_UpdatesProfileSuccessfully()
        {
            var context = GetDbContext();
            var profile = new Profile { UserId = 1, Name = "Old Name", Email = "old@example.com", Phone = "9999999999" };
            context.Profiles.Add(profile);
            await context.SaveChangesAsync();

            var controller = new ProfileController(context);
            var updatedProfile = new Profile { UserId = 1, Name = "Updated Name", Email = "updated@example.com", Phone = "8888888888" };

            var result = await controller.UpdateProfileByUserId(1, updatedProfile);

            Assert.IsType<NoContentResult>(result);

            var updated = await context.Profiles.FirstOrDefaultAsync(p => p.UserId == 1);
            Assert.Equal("Updated Name", updated.Name);
        }

        [Fact]
        public async Task DeleteProfile_RemovesProfile()
        {
            var context = GetDbContext();
            var profile = new Profile { Name = "Delete Me", Email = "delete@example.com", Phone = "2222222222" };
            context.Profiles.Add(profile);
            await context.SaveChangesAsync();

            var controller = new ProfileController(context);

            var result = await controller.DeleteProfile(profile.ProfileId);

            Assert.IsType<NoContentResult>(result);
            Assert.Empty(context.Profiles);
        }
    }
}
