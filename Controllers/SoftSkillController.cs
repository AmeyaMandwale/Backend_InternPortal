using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using InternConnect_Backend.Data; // adjust namespace
using InternConnect_Backend.Models;// adjust namespace
using Microsoft.Extensions.Configuration;
using InternConnect_Backend.Data;

namespace Backend_InternPortal.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SoftSkillController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _config;
        private readonly ApplicationDbContext _context;

        public SoftSkillController(IHttpClientFactory httpClientFactory, IConfiguration config, ApplicationDbContext context)
        {
            _httpClientFactory = httpClientFactory;
            _config = config;
            _context = context;
        }

        [HttpPost("track")]
        public async Task<IActionResult> TrackSoftSkills([FromBody] SoftSkillRequest request)
        {
            

            // 1. Fetch complete profile
            var profile = await _context.Profiles
                .Include(p => p.Educations)
                .Include(p => p.Skills)
                .Include(p => p.Projects)
                .Include(p => p.Achievements)
                .FirstOrDefaultAsync(p => p.UserId == request.UserId);

            if (profile == null)
                return NotFound("Profile not found for given UserId");

            // CareerGoal comes from profile, not request
            string careerGoal = profile.CareerGoal ?? "Not specified";

            // 2. Build conversation history
            var conversation = "";
            foreach (var msg in request.Messages)
            {
                if (msg.Sender == "user")
                    conversation += $"User: {msg.Text}\n";
                else if (msg.Sender == "bot")
                    conversation += $"Bot: {msg.Text}\n";
            }

            // 3. Decide if still asking questions or evaluation phase
            int userAnswersCount = request.Messages.Count(m => m.Sender == "user");

            string prompt;

            if (userAnswersCount < 1)
            {
                // still asking questions
                prompt = $@"
You are a Soft Skill Interview Assistant. 
You are asking the user 10 questions related to their profile and career goals to assess their communication skills. 
So far, {userAnswersCount} questions have been asked and answered and don't ask long question.

User Profile:
Name: {profile.Name}
Career Goal: {careerGoal}
Skills: {string.Join(", ", profile.Skills.Select(s => s.Technical))}

Conversation so far:
{conversation}

Now, ask the next question (Question {userAnswersCount + 1}). 
Do NOT repeat previous questions. 
Do NOT give a score or feedback yet, only ask the next question politely.";
            }
            else
            {
                // all 10 answers done, now score
                prompt = $@"
You are a Soft Skill Evaluator.
You have received answers from a user for 10 questions about their profile and career goals. 
Your job is to:
1. Provide a **score (integer between 1 and 10)** for communication skill (clarity, confidence, relevance). 
2. Write a **short feedback paragraph** (3–4 sentences max) pointing out mistakes and suggesting improvements. 
   - Always mention grammar/spelling if errors are present.
   - Keep it balanced: highlight 1 strength + 2 improvements.

User Profile:
Name: {profile.Name}
Career Goal: {careerGoal}
Skills: {string.Join(", ", profile.Skills.Select(s => s.Technical))}

Conversation history:
{conversation}

Now, generate output in strict JSON format:
{{
  ""score"": <integer between 1 and 10>,
  ""feedback"": ""<short feedback paragraph, 3–4 sentences maximum>""
}}";
            }


            // 4. Call Gemini API
            var geminiUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={_config["Gemini:ApiKey"]}";

            var payload = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
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
                return StatusCode((int)response.StatusCode, $"Failed to generate response from Gemini API. Response: {errorBody}");
            }

            var responseString = await response.Content.ReadAsStringAsync();
            dynamic geminiResponse = JsonConvert.DeserializeObject(responseString);

            if (geminiResponse?.candidates == null || geminiResponse.candidates.Count == 0)
                return BadRequest("No response generated by Gemini");

            string aiContent = geminiResponse.candidates[0].content.parts[0].text.ToString().Trim();

            if (userAnswersCount < 10)
            {
                // return next question
                return Ok(new { nextQuestion = aiContent });
            }
            else
            {
                // Parse JSON score + feedback
                try
                {
                    var resultObj = JsonConvert.DeserializeObject<SoftSkillResult>(aiContent);
                    return Ok(resultObj);
                }
                catch
                {
                    // fallback in case AI didn't return JSON
                    return Ok(new { raw = aiContent });
                }
            }
        }
    }

    public class SoftSkillRequest
    {
        public int UserId { get; set; }
        public List<SoftSkillMessageEntry> Messages { get; set; }
    }

    public class SoftSkillMessageEntry
    {
        public string Sender { get; set; }  // "user" or "bot"
        public string Text { get; set; }
    }

    public class SoftSkillResult
    {
        public int Score { get; set; }
        public string Feedback { get; set; }
    }
}
