//using System.Net.Http;
//using System.Threading.Tasks;
//using Newtonsoft.Json.Linq;
//using InternConnect_Backend.Models;
//using System.Collections.Generic;
//using System;
//using static System.Runtime.InteropServices.JavaScript.JSType;

//namespace InternConnect_Backend.Services
//{
//    public class SerpApiService
//    {
//        private readonly HttpClient _httpClient;
//        private readonly string _apiKey = "b2df3080c425f4e011a196d078ef280159ca2b64205747cd3c273df6f0ca3f83"; // move to appsettings.json for safety

//        public SerpApiService(HttpClient httpClient)
//        {
//            _httpClient = httpClient;
//        }

//        public async Task<List<Opportunity>> FetchInternships(string query, string location, int userId = 0)
//        {
//            var url = $"https://serpapi.com/search.json?engine=google_jobs&q={query}+{location}&hl=en&api_key={_apiKey}";
//            var response = await _httpClient.GetStringAsync(url);
//            var json = JObject.Parse(response);
//            var internships = new List<Opportunity>();

//            foreach (var job in json["jobs_results"] ?? new JArray())
//            {
//                internships.Add(new Opportunity
//                {
//                    UserId = null, // can be set dynamically later
//                    Position = job["title"]?.ToString() ?? string.Empty,
//                    Company = job["company_name"]?.ToString() ?? string.Empty,
//                    Location = job["location"]?.ToString() ?? string.Empty,
//                    Stipend = "", // SerpAPI doesn’t provide stipend, keep blank
//                    PostedDate = DateTime.UtcNow, // SerpAPI sometimes has "detected_extensions.posted_at"
//                    Description = job["description"]?.ToString() ?? string.Empty,
//                    IsSaved = false,
//                    IsApplied = false,
//                    Status = "Open", // default assumption
//                    ApplyLink = job["apply_options"]?.FirstOrDefault()?["link"]?.ToString() ?? string.Empty,
//                    Type = job["detected_extensions"]?["schedule_type"]?.ToString() ?? "Internship",
//                    CompanyWebsite = job["company_link"]?.ToString() ?? string.Empty
//                });
//            }

//            return internships;
//        }
//    }
//}


using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using InternConnect_Backend.Models;
using System.Collections.Generic;
using System;
using System.Linq;

namespace InternConnect_Backend.Services
{
    public class SerpApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey = "b2df3080c425f4e011a196d078ef280159ca2b64205747cd3c273df6f0ca3f83"; // ✅ move to appsettings.json

        public SerpApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<Opportunity>> FetchOpportunitiesAsync(string query, string location, int userId = 0)
        {
            // ✅ Force internships only by adding "internship" keyword
            var url = $"https://serpapi.com/search.json?engine=google_jobs&q={query}+internship+{location}&hl=en&api_key={_apiKey}";
            var response = await _httpClient.GetStringAsync(url);
            var json = JObject.Parse(response);

            var internships = new List<Opportunity>();

            foreach (var job in json["jobs_results"] ?? new JArray())
            {
                var detectedExtensions = job["detected_extensions"];

                internships.Add(new Opportunity
                {
                    UserId = userId,
                    Position = job["title"]?.ToString() ?? string.Empty,
                    Company = job["company_name"]?.ToString() ?? string.Empty,
                    Location = job["location"]?.ToString() ?? string.Empty,
                    Stipend = "", // SerpAPI doesn’t provide stipend
                    PostedDate = ParsePostedDate(detectedExtensions?["posted_at"]?.ToString()),
                    Description = job["description"]?.ToString() ?? string.Empty,
                    IsSaved = false,
                    IsApplied = false,
                    Status = "Open",
                    ApplyLink = job["apply_options"]?.FirstOrDefault()?["link"]?.ToString() ?? string.Empty,
                    Type = "Internship", // ✅ Force label as Internship
                    CompanyWebsite = job["company_link"]?.ToString() ?? string.Empty
                });
            }

            return internships;
        }


        // ✅ helper to safely parse posted date
        private DateTime ParsePostedDate(string postedAt)
        {
            if (string.IsNullOrEmpty(postedAt))
                return DateTime.UtcNow;

            // Example: "3 days ago", "30+ days ago"
            if (postedAt.Contains("day"))
            {
                var number = new string(postedAt.TakeWhile(char.IsDigit).ToArray());
                if (int.TryParse(number, out int days))
                    return DateTime.UtcNow.AddDays(-days);
            }
            if (postedAt.Contains("hour"))
            {
                var number = new string(postedAt.TakeWhile(char.IsDigit).ToArray());
                if (int.TryParse(number, out int hours))
                    return DateTime.UtcNow.AddHours(-hours);
            }

            return DateTime.UtcNow;
        }
    }
}
