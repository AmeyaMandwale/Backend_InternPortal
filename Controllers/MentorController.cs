using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InternConnect_Backend.Data;
using InternConnect_Backend.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using InternConnect_Backend.Services;

namespace Backend_InternPortal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MentorController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;

        public MentorController(ApplicationDbContext context, EmailService emailService)
        {
            _context = context;
            _emailService = emailService;

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

            // ✅ Send email based on approval
            string subject, body;

            if (approve)
            {
                subject = "🎉 Mentor Application Approved!";
                body = $@"
                    <h3>Dear {mentor.Name},</h3>
                    <p>Congratulations! Your application to become a mentor on <b>InternConnect</b> has been approved.</p>
                    <p>You can now log in and start mentoring students.</p>
                    <br/>
                    <p>Best regards,<br/>InternConnect Team</p>
                ";
            }
            else
            {
                subject = "❌ Mentor Application Rejected";
                body = $@"
                    <h3>Dear {mentor.Name},</h3>
                    <p>We regret to inform you that your application to become a mentor on <b>InternConnect</b> has been rejected at this time.</p>
                    <p>You may contact the admin for more details or reapply later.</p>
                    <br/>
                    <p>Best regards,<br/>InternConnect Team</p>
                ";
            }

            await _emailService.SendEmailAsync(mentor.Email, subject, body);

            return Ok(new { message = $"Mentor {(approve ? "approved" : "rejected")} and email sent successfully." });
        }
    }
}
