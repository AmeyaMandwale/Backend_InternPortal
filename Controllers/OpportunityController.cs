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

       

        // ✅ GET: api/Opportunity/user/3 → all opportunities of a user
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<Opportunity>>> GetOpportunitiesByUser(int userId)
        {
            var opportunities = await _context.Opportunities
                .Where(o => o.UserId == userId)
                .ToListAsync();

            return opportunities;
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





        //// Controller for SerpAPI


        //[HttpGet("fetch-by-careergoal/{userId}")]
        //public async Task<IActionResult> FetchByCareerGoal(int userId)
        //{
        //    // Get the user’s profile
        //    var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == userId);

        //    if (profile == null || string.IsNullOrEmpty(profile.CareerGoal))
        //        return NotFound("Profile or CareerGoal not found for this user");

        //    string query = profile.CareerGoal;
        //    string location = "India"; // fixed location

        //    // Fetch internships from external API (limit 10 results)
        //    var opportunities = (await _serpApiService.FetchOpportunitiesAsync(query, location, userId))
        //        .Take(10)
        //        .ToList();

        //    // ✅ Fetch ALL existing opportunities for this user
        //    var existingOpps = await _context.Opportunities
        //        .Where(o => o.UserId == userId)
        //        .ToListAsync();

        //    // Build a set of unique keys for faster lookup
        //    var existingKeys = new HashSet<string>(
        //        existingOpps.Select(o => $"{o.Position}-{o.Company}-{o.ApplyLink}"),
        //        StringComparer.OrdinalIgnoreCase
        //    );

        //    var newOpportunities = new List<Opportunity>();

        //    foreach (var opp in opportunities)
        //    {
        //        string key = $"{opp.Position}-{opp.Company}-{opp.ApplyLink}";

        //        if (!existingKeys.Contains(key))
        //        {
        //            newOpportunities.Add(opp);
        //            existingKeys.Add(key); // avoid dupes in same batch
        //        }
        //    }

        //    // Save only non-duplicate new ones
        //    if (newOpportunities.Any())
        //    {
        //        _context.Opportunities.AddRange(newOpportunities);
        //        await _context.SaveChangesAsync();
        //    }

        //    // ✅ Now fetch ALL opportunities again (including saved ones) 
        //    // and filter them by fuzzy CareerGoal matching
        //    var allOpportunities = await _context.Opportunities
        //        .Where(o => o.UserId == userId)
        //        .ToListAsync();

        //    var finalList = allOpportunities
        //        .Where(o =>
        //            o.Position != null &&
        //            profile.CareerGoal != null &&
        //            (
        //                // fuzzy contains
        //                o.Position.Contains(profile.CareerGoal, StringComparison.OrdinalIgnoreCase) ||

        //                // partial token-based match (e.g., "DevOps" matches "DevOps Intern")
        //                profile.CareerGoal.Split(' ')
        //                    .Any(token => o.Position.Contains(token, StringComparison.OrdinalIgnoreCase))
        //            )
        //        )
        //        .OrderByDescending(o => o.PostedDate)
        //        .Take(10)
        //        .ToList();

        //    return Ok(finalList);
        //}

        [HttpGet("fetch-by-careergoal/{userId}")]
        public async Task<IActionResult> FetchByCareerGoal(int userId)
        {
            var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null || string.IsNullOrEmpty(profile.CareerGoal))
                return NotFound("Profile or CareerGoal not found for this user");

            string query = profile.CareerGoal;
            string location = "India";

            // Fetch internships from external API (⚠️ removed Take(10) here)
            var opportunities = (await _serpApiService.FetchOpportunitiesAsync(query, location, userId))
                .Take(10)
                .ToList();

            // ✅ Fetch ALL existing opportunities for this user
            var existingOpps = await _context.Opportunities
                .Where(o => o.UserId == userId)
                .ToListAsync();

            // Build a set of unique keys for faster lookup
            var existingKeys = new HashSet<string>(
                existingOpps.Select(o => $"{o.Position}-{o.Company}-{o.ApplyLink}"),
                StringComparer.OrdinalIgnoreCase
            );

            var newOpportunities = new List<Opportunity>();

            foreach (var opp in opportunities)
            {
                string key = $"{opp.Position}-{opp.Company}-{opp.ApplyLink}";

                if (!existingKeys.Contains(key))
                {
                    newOpportunities.Add(opp);
                    existingKeys.Add(key); // avoid dupes in same batch
                }
            }

            // Save only non-duplicate new ones
            if (newOpportunities.Any())
            {
                _context.Opportunities.AddRange(newOpportunities);
                await _context.SaveChangesAsync();
            }

            // ✅ Fetch ALL opportunities again (including saved ones) 
            var allOpportunities = await _context.Opportunities
                .Where(o => o.UserId == userId)
                .ToListAsync();

            var finalList = allOpportunities
    .Where(o =>
        o.Position != null &&
        profile.CareerGoal != null &&
        (
            o.Position.Contains(profile.CareerGoal, StringComparison.OrdinalIgnoreCase) ||
            profile.CareerGoal.Split(' ')
                .Any(token => o.Position.Contains(token, StringComparison.OrdinalIgnoreCase))
        )
    )
    .OrderByDescending(o => o.PostedDate)
    .Take(10)
   .Select(o => new {
       opportunityId = o.OpportunityId,
       position = o.Position,
       company = o.Company,
       location = o.Location,
       stipend = o.Stipend,
       description = o.Description,
       posted = o.PostedDate != DateTime.MinValue
        ? o.PostedDate.ToString("o") // ✅ correct handling
        : null,
       isSaved = o.IsSaved,
       isApplied = o.IsApplied,
       status = o.Status,
       type = o.Type,
       applyLink = o.ApplyLink,
       companyWebsite = o.CompanyWebsite
   })
.ToList();


            return Ok(finalList);

        }










        // 🔎 Helper
        private bool OpportunityExists(int id)
        {
            return _context.Opportunities.Any(e => e.OpportunityId == id);
        }
    }
}
