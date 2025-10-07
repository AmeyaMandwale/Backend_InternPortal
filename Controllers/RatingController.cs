using InternConnect_Backend.Data;
using InternConnect_Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend_InternPortal.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RatingController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RatingController(ApplicationDbContext context)
        {
            _context = context;
        }

        // POST api/rating
        [HttpPost]
        public async Task<IActionResult> SubmitRating([FromBody] ApplicationRating rating)
        {
            if (rating == null || rating.Rating < 1 || rating.Rating > 5)
                return BadRequest(new { message = "Invalid rating" });

            // Use authenticated user if available
            var userId = User?.Identity?.Name ?? rating.UserId ?? "guest";

            var existing = await _context.ApplicationRatings
                .FirstOrDefaultAsync(r => r.UserId == userId);

            if (existing != null)
            {
                existing.Rating = rating.Rating;
                existing.RatedOn = DateTime.UtcNow;
                _context.ApplicationRatings.Update(existing);
            }
            else
            {
                rating.UserId = userId;
                rating.RatedOn = DateTime.UtcNow;
                _context.ApplicationRatings.Add(rating);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Rating submitted" });
        }
        [HttpGet("user/{userid}")]
        public async Task<IActionResult> GetUserRating(string userid)
        {
            var rating = await _context.ApplicationRatings
                .FirstOrDefaultAsync(r => r.UserId == userid);

            if (rating == null)
                return Ok(new { rating = 0 });

            return Ok(new { rating = rating.Rating });
        }

        // GET api/rating
        [HttpGet]
        public async Task<IActionResult> GetRatingStats()
        {
            var ratings = await _context.ApplicationRatings.ToListAsync();
            if (!ratings.Any())
                return Ok(new { Average = 0.0, Count = 0 });

            var avg = Math.Round(ratings.Average(r => r.Rating), 2);
            var count = ratings.Count;
            return Ok(new { Average = avg, Count = count });
        }
    }
}
