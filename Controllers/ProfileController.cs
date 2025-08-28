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

        // ✅ Get Profile by Id (with nested data)
        [HttpGet("{id}")]
        public async Task<ActionResult<Profile>> GetProfile(int id)
        {
            var profile = await _context.Profiles
                .Include(p => p.Educations)
                .Include(p => p.Skills)
                .Include(p => p.Projects)
                .Include(p => p.Achievements)
                .FirstOrDefaultAsync(p => p.ProfileId == id);

            if (profile == null)
                return NotFound();

            return profile;
        }

        // ✅ Create Profile with nested entities
        [HttpPost]
        public async Task<ActionResult<Profile>> CreateProfile(Profile profile)
        {
            _context.Profiles.Add(profile);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetProfile), new { id = profile.ProfileId }, profile);
        }

        // ✅ Update Profile with nested entities
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProfile(int id, Profile profile)
        {
            if (id != profile.ProfileId)
                return BadRequest();

            var existingProfile = await _context.Profiles
                .Include(p => p.Educations)
                .Include(p => p.Skills)
                .Include(p => p.Projects)
                .Include(p => p.Achievements)
                .FirstOrDefaultAsync(p => p.ProfileId == id);

            if (existingProfile == null)
                return NotFound();

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
