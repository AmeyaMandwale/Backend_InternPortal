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
    public class SkillGapAnalysesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;

        public SkillGapAnalysesController(ApplicationDbContext context, IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _config = config;
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateSkillGapAnalyses([FromBody] GenerateSkillGapAnalysesRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.SkillGapAnalysisGoal))
                return BadRequest("SkillGapAnalysisGoal cannot be empty.");

            // 1. Fetch complete profile
            var profile = await _context.Profiles
                .Include(p => p.Educations)
                .Include(p => p.Skills)
                .Include(p => p.Projects)
                .Include(p => p.Achievements)
                .FirstOrDefaultAsync(p => p.UserId == request.UserId);

            if (profile == null)
                return NotFound("Profile not found for given UserId");

            // 2. Delete existing analyses for the current SkillGapAnalysisGoal
            var existingAnalyses = _context.SkillGapAnalyses
                .Where(s => s.UserId == request.UserId && s.SkillGapAnalysisGoal == request.SkillGapAnalysisGoal);

            if (existingAnalyses.Any())
            {
                _context.SkillGapAnalyses.RemoveRange(existingAnalyses);
                await _context.SaveChangesAsync();
            }

            // 3. Delete obsolete analyses not in current CareerGoal and not equal to current SkillGapAnalysisGoal
            if (!string.IsNullOrWhiteSpace(profile.CareerGoal))
            {
                var activeGoals = profile.CareerGoal
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(g => g.Trim())
                    .ToList();

                var obsoleteAnalyses = _context.SkillGapAnalyses
                    .Where(s => s.UserId == request.UserId
                                && !activeGoals.Contains(s.SkillGapAnalysisGoal)
                                && s.SkillGapAnalysisGoal != request.SkillGapAnalysisGoal);

                if (obsoleteAnalyses.Any())
                {
                    _context.SkillGapAnalyses.RemoveRange(obsoleteAnalyses);
                    await _context.SaveChangesAsync();
                }
            }

            // 4. Serialize profile safely
            var profileJson = JsonConvert.SerializeObject(profile,
                Formatting.None,
                new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore });

            // 5. Build user prompt
            var userPrompt = $@"
Generate {request.Count} skill gap analyses for the following profile and goal.
Each analysis must follow this JSON schema and keep SkillGapAnalysisId = 0 every time :

[
  {{
    ""SkillGapAnalysisId"": 0,
    ""UserId"": {request.UserId},
    ""Title"": ""string"",
    ""Description"": ""string"",
    ""ResourceLink"": ""string"",
    ""SkillGapAnalysisGoal"": ""{request.SkillGapAnalysisGoal}""
  }}
]

Profile Data: {profileJson}
Skill Gap Analysis Goal: {request.SkillGapAnalysisGoal}
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
                return StatusCode((int)response.StatusCode, $"Failed to generate skill gap analyses from Gemini API. Response: {errorBody}");
            }

            var responseString = await response.Content.ReadAsStringAsync();
            dynamic geminiResponse = JsonConvert.DeserializeObject(responseString);

            if (geminiResponse?.candidates == null || geminiResponse.candidates.Count == 0)
                return BadRequest("No skill gap analyses generated by Gemini");

            string aiContent = geminiResponse.candidates[0].content.parts[0].text.ToString();

            // 7. Clean AI response (strip ```json fences if present)
            aiContent = aiContent.Trim();
            if (aiContent.StartsWith("```"))
            {
                int firstNewLine = aiContent.IndexOf('\n');
                int lastFence = aiContent.LastIndexOf("```");
                if (firstNewLine >= 0 && lastFence > firstNewLine)
                    aiContent = aiContent.Substring(firstNewLine + 1, lastFence - firstNewLine - 1).Trim();
            }

            // 8. Deserialize into SkillGapAnalysis
            List<SkillGapAnalysis>? analyses;
            try
            {
                analyses = JsonConvert.DeserializeObject<List<SkillGapAnalysis>>(aiContent);
            }
            catch (JsonException ex)
            {
                return BadRequest("Failed to parse skill gap analyses from Gemini response. AI response: " + aiContent + " | Error: " + ex.Message);
            }

            if (analyses == null || !analyses.Any())
                return BadRequest("No skill gap analyses parsed from AI response");

            // 9. Ensure UserId, SkillGapAnalysisGoal, and defaults
            foreach (var analysis in analyses)
            {
                analysis.UserId = request.UserId;
                analysis.SkillGapAnalysisGoal = request.SkillGapAnalysisGoal;
            }

            // 10. Save analyses to DB
            _context.SkillGapAnalyses.AddRange(analyses);
            await _context.SaveChangesAsync();

            // 11. Return saved analyses
            return Ok(analyses);
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetSkillGapAnalysesByUserId(int userId)
        {
            var analyses = await _context.SkillGapAnalyses
                .Where(s => s.UserId == userId)
                .ToListAsync();

            if (analyses == null || !analyses.Any())
                return NotFound(new { message = $"No Skill Gap Analyses found for UserId {userId}" });

            return Ok(analyses);
        }
    }

    public class GenerateSkillGapAnalysesRequest
    {
        public int UserId { get; set; }
        public int Count { get; set; } = 5;
        public string SkillGapAnalysisGoal { get; set; } = string.Empty;
    }
}
