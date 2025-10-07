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
    public class MentorController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MentorController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ✅ Get All Mentors
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Mentor>>> GetMentors()
        {
            return await _context.Mentors.ToListAsync();
        }

        // ✅ Get Mentor by ID
        [HttpGet("{id}")]
        public async Task<ActionResult<Mentor>> GetMentorById(int id)
        {
            var mentor = await _context.Mentors.FindAsync(id);

            if (mentor == null)
                return NotFound(new { message = $"No mentor found with ID {id}" });

            return mentor;
        }

        // ✅ Create Mentor
        [HttpPost]
        public async Task<ActionResult<Mentor>> CreateMentor(Mentor mentor)
        {
            _context.Mentors.Add(mentor);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMentorById), new { id = mentor.MentorId }, mentor);
        }

        // ✅ Update Mentor by ID
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMentor(int id, Mentor mentor)
        {
            var existingMentor = await _context.Mentors.FindAsync(id);

            if (existingMentor == null)
                return NotFound(new { message = $"No mentor found with ID {id}" });

            // --- Update scalar fields ---
            _context.Entry(existingMentor).CurrentValues.SetValues(mentor);

            // ✅ Keep old photo if frontend sends empty string
            if (string.IsNullOrEmpty(mentor.Photo))
            {
                _context.Entry(existingMentor).Property(m => m.Photo).IsModified = false;
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // ✅ Delete Mentor by ID
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMentor(int id)
        {
            var mentor = await _context.Mentors.FindAsync(id);

            if (mentor == null)
                return NotFound(new { message = $"No mentor found with ID {id}" });

            _context.Mentors.Remove(mentor);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // ✅ Get Only Approved Mentors
        [HttpGet("approved")]
        public async Task<ActionResult<IEnumerable<Mentor>>> GetApprovedMentors()
        {
            return await _context.Mentors.Where(m => m.Approved == true).ToListAsync();
        }

        // ✅ Approve or Reject Mentor
        [HttpPut("{id}/approve")]
        public async Task<IActionResult> ApproveMentor(int id, [FromQuery] bool approve)
        {
            var mentor = await _context.Mentors.FindAsync(id);

            if (mentor == null)
                return NotFound(new { message = $"No mentor found with ID {id}" });

            mentor.Approved = approve;
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Mentor {(approve ? "approved" : "rejected")} successfully." });
        }
    }
}
