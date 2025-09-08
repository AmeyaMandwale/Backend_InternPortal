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
public class OpportunityRefreshService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _interval = TimeSpan.FromHours(24); // run every 24 hrs
    private readonly TimeSpan _scheduledTime = new TimeSpan(10, 41, 0); // 2 AM IST

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

            if (now > nextRun) // if 2 AM already passed, schedule for tomorrow
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
            await context.SaveChangesAsync(stoppingToken); // commit deletion
        }

        // ✅ Step 2: Refresh for each user
        var users = await context.Profiles.ToListAsync(stoppingToken);

        foreach (var user in users)
        {
            if (!string.IsNullOrEmpty(user.CareerGoal))
            {
                var freshOpps = await serpApiService.FetchOpportunitiesAsync(
                    user.CareerGoal, "India", user.UserId);

                foreach (var opp in freshOpps.Take(10))
                {
                    // prevent duplicates based on ApplyLink
                    bool exists = await context.Opportunities.AnyAsync(o =>
                        o.UserId == user.UserId &&
                        o.ApplyLink == opp.ApplyLink, stoppingToken);

                    if (!exists)
                    {
                        context.Opportunities.Add(opp);
                    }
                }
            }
        }

        await context.SaveChangesAsync(stoppingToken); // commit inserts
    }

}
