



using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InternConnect_Backend.Models;
using InternConnect_Backend.Services;
using InternConnect_Backend.Data;
using System.Collections.Generic;

public class OpportunityRefreshService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _interval = TimeSpan.FromHours(24); // run every 24 hrs
    private readonly TimeSpan _scheduledTime = new TimeSpan(10, 45, 0); // 2 AM IST

    public OpportunityRefreshService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow.AddHours(5.5); // convert UTC → IST
            var nextRun = DateTime.Today.Add(_scheduledTime);

            if (now > nextRun) // if scheduled time already passed, schedule for tomorrow
                nextRun = nextRun.AddDays(1);

            var delay = nextRun - now;
            await Task.Delay(delay, stoppingToken);

            await RunJob(stoppingToken);

            // wait 24 hrs before next run
            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task RunJob(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var serpApiService = scope.ServiceProvider.GetRequiredService<SerpApiService>();

        // ✅ Step 1: Delete non-saved, non-applied opportunities
        var oldOpps = await context.Opportunities
            .Where(o => !o.IsSaved && !o.IsApplied)
            .ToListAsync(stoppingToken);

        if (oldOpps.Any())
        {
            context.Opportunities.RemoveRange(oldOpps);
            await context.SaveChangesAsync(stoppingToken);
        }

        // ✅ Step 2: Refresh for each user
        var users = await context.Profiles.ToListAsync(stoppingToken);

        foreach (var user in users)
        {
            if (!string.IsNullOrEmpty(user.CareerGoal))
            {
                var careerGoals = user.CareerGoal
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(c => c.Trim())
                    .Where(c => !string.IsNullOrEmpty(c))
                    .ToList();

                // decide allocation
                var allocations = new List<int>();
                if (careerGoals.Count == 1)
                {
                    allocations.Add(10);
                }
                else if (careerGoals.Count == 2)
                {
                    allocations.AddRange(new[] { 5, 5 });
                }
                else if (careerGoals.Count >= 3)
                {
                    var rand = new Random();
                    var distributions = new List<int[]> {
                        new[] {4, 3, 3},
                        new[] {3, 4, 3},
                        new[] {3, 3, 4}
                    };
                    allocations.AddRange(distributions[rand.Next(distributions.Count)]);
                }

                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // fetch per goal with allocation
                for (int i = 0; i < allocations.Count; i++)
                {
                    var goal = careerGoals[i];
                    var countNeeded = allocations[i];
                    int addedCount = 0;

                    var rawFresh = await serpApiService.FetchOpportunitiesAsync(goal, "India", user.UserId);
                    if (rawFresh == null) rawFresh = new List<Opportunity>();

                    foreach (var opp in rawFresh)
                    {
                        var key = !string.IsNullOrWhiteSpace(opp.ApplyLink)
                            ? NormalizeLink(opp.ApplyLink)
                            : BuildPosCompKey(opp.Position, opp.Company);

                        if (string.IsNullOrWhiteSpace(key)) continue;
                        if (seen.Contains(key)) continue;

                        bool exists = await context.Opportunities.AnyAsync(o =>
                            o.UserId == user.UserId &&
                            (o.ApplyLink == opp.ApplyLink ||
                             (o.Position == opp.Position && o.Company == opp.Company)),
                             stoppingToken);

                        if (exists) continue;

                        opp.UserId = user.UserId;
                        opp.IsSaved = false;
                        opp.IsApplied = false;
                        opp.Status = "None";
                        context.Opportunities.Add(opp);

                        seen.Add(key);
                        addedCount++;

                        if (addedCount >= countNeeded)
                            break;
                    }
                }
            }
        }

        await context.SaveChangesAsync(stoppingToken);
    }

    // --- Helpers (same as in controller) ---
    private static string NormalizeLink(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return "";
        url = url.Trim().ToLowerInvariant();
        var q = url.IndexOf('?');
        if (q >= 0) url = url.Substring(0, q);
        url = url.TrimEnd('/');
        return url;
    }

    private static string NormalizeText(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        s = s.Trim().ToLowerInvariant();
        s = System.Text.RegularExpressions.Regex.Replace(s, @"\s+", " ");
        s = System.Text.RegularExpressions.Regex.Replace(s, @"[^a-z0-9\s]", "");
        return s;
    }

    private static string BuildPosCompKey(string pos, string comp)
        => $"{NormalizeText(pos)}|{NormalizeText(comp)}";
}

