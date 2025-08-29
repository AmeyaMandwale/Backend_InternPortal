




using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InternConnect_Backend.Data;
using InternConnect_Backend.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Backend_InternPortal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProfileController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ProfileController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ✅ Get All Profiles (with nested data)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Profile>>> GetProfiles()
        {
            return await _context.Profiles
                .Include(p => p.Educations)
                .Include(p => p.Skills)
                .Include(p => p.Projects)
                .Include(p => p.Achievements)
                .ToListAsync();
        }

        // ✅ Get Profile by ProfileId
        [HttpGet("{id}")]
        public async Task<ActionResult<Profile>> GetProfileById(int id)
        {
            var profile = await _context.Profiles
                .Include(p => p.Educations)
                .Include(p => p.Skills)
                .Include(p => p.Projects)
                .Include(p => p.Achievements)
                .FirstOrDefaultAsync(p => p.ProfileId == id);

            if (profile == null)
                return NotFound(new { message = $"No profile found with ProfileId {id}" });

            return profile;
        }

        // ✅ Get Profile by UserId
        [HttpGet("user/{userid}")]
        public async Task<ActionResult<Profile>> GetProfileByUserId(int userid)
        {
            var profile = await _context.Profiles
                .Include(p => p.Educations)
                .Include(p => p.Skills)
                .Include(p => p.Projects)
                .Include(p => p.Achievements)
                .FirstOrDefaultAsync(p => p.UserId == userid);

            if (profile == null)
                return NotFound(new { message = $"No profile found for UserId {userid}" });

            return profile;
        }

        // ✅ Create Profile with nested entities
        [HttpPost]
        public async Task<ActionResult<Profile>> CreateProfile(Profile profile)
        {
            _context.Profiles.Add(profile);
            await _context.SaveChangesAsync();

            // ✅ Now points to GetProfileById (ProfileId route)
            return CreatedAtAction(nameof(GetProfileById), new { id = profile.ProfileId }, profile);
        }

        // ✅ Update Profile with nested entities (by UserId)
        [HttpPut("{userid}")]
        public async Task<IActionResult> UpdateProfileByUserId(int userid, Profile profile)
        {
            var existingProfile = await _context.Profiles
                .Include(p => p.Educations)
                .Include(p => p.Skills)
                .Include(p => p.Projects)
                .Include(p => p.Achievements)
                .FirstOrDefaultAsync(p => p.UserId == userid);

            if (existingProfile == null)
                return NotFound(new { message = $"No profile found for UserId {userid}" });

            // --- Update scalar fields ---
            _context.Entry(existingProfile).CurrentValues.SetValues(profile);

            // --- Update Educations ---
            existingProfile.Educations.Clear();
            foreach (var edu in profile.Educations)
            {
                existingProfile.Educations.Add(edu);
            }

            // --- Update Skills ---
            existingProfile.Skills.Clear();
            foreach (var skill in profile.Skills)
            {
                existingProfile.Skills.Add(skill);
            }

            // --- Update Projects ---
            existingProfile.Projects.Clear();
            foreach (var proj in profile.Projects)
            {
                existingProfile.Projects.Add(proj);
            }

            // --- Update Achievements ---
            existingProfile.Achievements.Clear();
            foreach (var ach in profile.Achievements)
            {
                existingProfile.Achievements.Add(ach);
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ✅ Delete Profile (and nested entities by cascade)
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProfile(int id)
        {
            var profile = await _context.Profiles
                .Include(p => p.Educations)
                .Include(p => p.Skills)
                .Include(p => p.Projects)
                .Include(p => p.Achievements)
                .FirstOrDefaultAsync(p => p.ProfileId == id);

            if (profile == null)
                return NotFound();

            _context.Profiles.Remove(profile);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
