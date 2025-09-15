




using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using InternConnect_Backend.Models;
using InternConnect_Backend.Data;

namespace Backend_InternPortal.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ActivitiesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public ActivitiesController(ApplicationDbContext context, IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateActivities([FromBody] GenerateActivitiesRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ActivityGoal))
                return BadRequest("ActivityGoal cannot be empty.");

            // 1. Fetch complete profile
            var profile = await _context.Profiles
                .Include(p => p.Educations)
                .Include(p => p.Skills)
                .Include(p => p.Projects)
                .Include(p => p.Achievements)
                .FirstOrDefaultAsync(p => p.UserId == request.UserId);

            if (profile == null)
                return NotFound("Profile not found for given UserId");

            // 2. Delete existing activities for the current ActivityGoal
            var existingActivities = _context.Activities
                .Where(a => a.UserId == request.UserId && a.ActivityGoal == request.ActivityGoal);

            if (existingActivities.Any())
            {
                _context.Activities.RemoveRange(existingActivities);
                await _context.SaveChangesAsync();
            }

            // 3. Delete obsolete activities not in current CareerGoal and not the current ActivityGoal
            if (!string.IsNullOrWhiteSpace(profile.CareerGoal))
            {
                var activeGoals = profile.CareerGoal
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(g => g.Trim())
                    .ToList();

                var obsoleteActivities = _context.Activities
                    .Where(a => a.UserId == request.UserId
                                && !activeGoals.Contains(a.ActivityGoal)
                                && a.ActivityGoal != request.ActivityGoal);

                if (obsoleteActivities.Any())
                {
                    _context.Activities.RemoveRange(obsoleteActivities);
                    await _context.SaveChangesAsync();
                }
            }

            // 4. Serialize profile safely
            var profileJson = JsonConvert.SerializeObject(profile,
                Formatting.None,
                new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });

            // 5. Build user prompt
            var userPrompt = $@"
Generate {request.Count} career development activities for the following profile and goal.
Each activity must follow this JSON schema and keep ActivityId = 0 everytime :

[
  {{
    ""ActivityId"": 0,
    ""UserId"": {request.UserId},
    ""StepNumber"": number from 1 to {request.Count},
    ""Heading"": ""string"",
    ""Description"": ""string"",
    ""ResourceLink"": ""string"",
    ""EstimatedTime"": ""string"",
    ""Status"": ""Pending"",
    ""ActivityGoal"": ""{request.ActivityGoal}""
  }}
]

Profile Data: {profileJson}
Activity Goal: {request.ActivityGoal}
";

            // 6. Call Gemini API
            var geminiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_config["Gemini:ApiKey"]}";

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = userPrompt }
                        }
                    }
                }
            };

            var httpClient = _httpClientFactory.CreateClient();
            var httpContent = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(geminiUrl, httpContent);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, $"Failed to generate activities from Gemini API. Response: {errorBody}");
            }

            var responseString = await response.Content.ReadAsStringAsync();
            dynamic geminiResponse = JsonConvert.DeserializeObject(responseString);

            if (geminiResponse?.candidates == null || geminiResponse.candidates.Count == 0)
                return BadRequest("No activities generated by Gemini");

            string aiContent = geminiResponse.candidates[0].content.parts[0].text.ToString();

            // 7. Clean AI response
            aiContent = aiContent.Trim();
            if (aiContent.StartsWith("```"))
            {
                int firstNewLine = aiContent.IndexOf('\n');
                int lastFence = aiContent.LastIndexOf("```");
                if (firstNewLine >= 0 && lastFence > firstNewLine)
                    aiContent = aiContent.Substring(firstNewLine + 1, lastFence - firstNewLine - 1).Trim();
            }

            // 8. Deserialize into Activities
            List<Activity>? activities;
            try
            {
                activities = JsonConvert.DeserializeObject<List<Activity>>(aiContent);
            }
            catch (JsonException ex)
            {
                return BadRequest("Failed to parse activities from Gemini response. AI response: " + aiContent + " | Error: " + ex.Message);
            }

            if (activities == null || !activities.Any())
                return BadRequest("No activities parsed from AI response");

            // 9. Ensure UserId, ActivityGoal, and defaults
            foreach (var activity in activities)
            {
                activity.UserId = request.UserId;
                activity.ActivityGoal = request.ActivityGoal;
                if (string.IsNullOrWhiteSpace(activity.Status))
                    activity.Status = "Pending";
            }

            // 10. Save activities to DB
            _context.Activities.AddRange(activities);
            await _context.SaveChangesAsync();

            // 11. Return saved activities
            return Ok(activities);
        }



        [HttpGet("{userId}")]
        public async Task<IActionResult> GetActivitiesByUserId(int userId)
        {
            var activities = await _context.Activities
                .Where(a => a.UserId == userId)
                .OrderBy(a => a.StepNumber)
                .ToListAsync();

            if (activities == null || !activities.Any())
                return NotFound(new { message = $"No Activity found for UserId {userId}" });

            return Ok(activities);
        }





        [HttpPut("{userId}/{activityId}")]
        public async Task<IActionResult> UpdateActivityStatus(int userId, int activityId, [FromBody] UpdateStatusRequest request)
        {
            try
            {
                // Find the activity by userId and activityId
                var activity = await _context.Activities
                    .FirstOrDefaultAsync(a => a.UserId == userId && a.ActivityId == activityId);

                if (activity == null)
                    return NotFound($"Activity with ID {activityId} for User ID {userId} not found");

                // Update the status
                activity.Status = request.Status;

                // Save changes to the database
                await _context.SaveChangesAsync();

                return Ok(activity);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while updating the activity status: {ex.Message}");
            }
        }



    }

    public class GenerateActivitiesRequest
    {
        public int UserId { get; set; }       // The user for whom activities will be generated
        public int Count { get; set; } = 5;   // Number of activities to generate (default 5)
        public string ActivityGoal { get; set; } = string.Empty;
    }

    public class UpdateStatusRequest
    {
        public string Status { get; set; }
    }
}
