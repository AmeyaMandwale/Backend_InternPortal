using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InternConnect_Backend.Data;
using InternConnect_Backend.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InternConnect_Backend.Services;

namespace Backend_Project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OpportunityController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly SerpApiService _serpApiService;


        private readonly string[] _validStatuses = new[] { "None", "Pending", "Approved", "Rejected" };

        public OpportunityController(ApplicationDbContext context, SerpApiService serpApiService)
        {
            _context = context;
            _serpApiService = serpApiService;

        }

        // ✅ GET: api/Opportunity
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Opportunity>>> GetOpportunities()
        {
            return await _context.Opportunities.ToListAsync();
        }

        // ✅ GET: api/Opportunity/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Opportunity>> GetOpportunity(int id)
        {
            var opportunity = await _context.Opportunities.FindAsync(id);

            if (opportunity == null)
            {
                return NotFound(new { message = "Opportunity not found." });
            }

            return opportunity;
        }

        // ✅ GET: api/Opportunity/user/3 → all opportunities of a user
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<Opportunity>>> GetOpportunitiesByUser(int userId)
        {
            var opportunities = await _context.Opportunities
                .Where(o => o.UserId == userId)
                .ToListAsync();

            return opportunities;
        }

        // ✅ POST: api/Opportunity
        [HttpPost]
        public async Task<ActionResult<Opportunity>> CreateOpportunity(Opportunity opportunity)
        {
            // validate Status
            if (!_validStatuses.Contains(opportunity.Status))
            {
                opportunity.Status = "None";
            }

            _context.Opportunities.Add(opportunity);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetOpportunity), new { id = opportunity.OpportunityId }, opportunity);
        }

        // ✅ PUT: api/Opportunity/5
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateOpportunity(int id, Opportunity updatedOpportunity)
        {
            if (id != updatedOpportunity.OpportunityId)
            {
                return BadRequest(new { message = "ID mismatch." });
            }

            var existingOpportunity = await _context.Opportunities.FindAsync(id);
            if (existingOpportunity == null)
            {
                return NotFound(new { message = "Opportunity not found." });
            }

            // ✅ Update only the fields we want to allow modification
            existingOpportunity.IsSaved = updatedOpportunity.IsSaved;
            existingOpportunity.IsApplied = updatedOpportunity.IsApplied;
            existingOpportunity.Status = updatedOpportunity.Status;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OpportunityExists(id))
                {
                    return NotFound(new { message = "Opportunity not found." });
                }
                else
                {
                    throw;
                }
            }

            return Ok(new
            {
                message = "Opportunity updated successfully.",
                data = existingOpportunity
            });
        }



        // ✅ DELETE: api/Opportunity/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOpportunity(int id)
        {
            var opportunity = await _context.Opportunities.FindAsync(id);
            if (opportunity == null)
            {
                return NotFound(new { message = "Opportunity not found." });
            }

            _context.Opportunities.Remove(opportunity);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        //// Controller for SerpAPI
        //[ApiController]
        //[Route("api/[controller]")]
        //public class InternshipsController : ControllerBase
        //{
        //    private readonly SerpApiService _serpApiService;

        //    public InternshipsController(SerpApiService serpApiService)
        //    {
        //        _serpApiService = serpApiService;
        //    }

        //    [HttpGet("fetch")]
        //    public async Task<IActionResult> Fetch([FromQuery] string profile, [FromQuery] string location)
        //    {
        //        var data = await _serpApiService.FetchInternships(profile, location);
        //        return Ok(data);
        //    }
        //}

        //[HttpGet("fetch")]
        //public async Task<ActionResult<IEnumerable<Opportunity>>> FetchFromSerpApi(
        //   string query = "DevOps Engineer", string location = "India")
        //{
        //    var internships = await _serpApiService.FetchInternships(query, location);

        //    // (Optional) Save them to your DB
        //    foreach (var opp in internships)
        //    {
        //        // Avoid duplicates (basic check)
        //        if (!_context.Opportunities.Any(o =>
        //                o.Position == opp.Position &&
        //                o.Company == opp.Company &&
        //                o.Location == opp.Location))
        //        {
        //            _context.Opportunities.Add(opp);
        //        }
        //    }

        //    await _context.SaveChangesAsync();

        //    return Ok(internships);
        //}

        [HttpGet("fetch-by-careergoal/{userId}")]
        public async Task<IActionResult> FetchByCareerGoal(int userId)
        {
            // Get the user’s profile
            var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null || string.IsNullOrEmpty(profile.CareerGoal))
                return NotFound("Profile or CareerGoal not found for this user");

            // Use career goal as search query
            string query = profile.CareerGoal;
            string location = string.IsNullOrEmpty(profile.Location) ? "India" : profile.Location; // fallback to India

            var opportunities = await _serpApiService.FetchOpportunitiesAsync(query, location, userId);

            // Attach UserId to opportunities
            foreach (var opp in opportunities)
            {
                opp.UserId = userId;
            }

            // Save to DB
            _context.Opportunities.AddRange(opportunities);
            await _context.SaveChangesAsync();

            return Ok(opportunities);
        }


        //// ✅ Fetch internships from RapidAPI (internship-only data source)
        //[HttpGet("fetch-internships/{userId}")]
        //public async Task<IActionResult> FetchInternshipsFromRapidApi(int userId)
        //{
        //    // ✅ Include Profile to access CareerGoal
        //    var userProfile = await _context.Profiles
        //        .FirstOrDefaultAsync(p => p.UserId == userId);

        //    if (userProfile == null)
        //        return NotFound("Profile not found for this user.");

        //    string careerGoal = userProfile.CareerGoal;

        //    // ✅ Pass careerGoal to service
        //    var internships = await _rapidApiService.FetchInternshipsAsync(careerGoal, "India", userId);

        //    return Ok(internships);
        //}



        // 🔎 Helper
        private bool OpportunityExists(int id)
        {
            return _context.Opportunities.Any(e => e.OpportunityId == id);
        }
    }
}
