using docDOC.Infrastructure.Persistence;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace docDOC.Infrastructure.Services.HangfireJobs;

public class RefreshTokenCleanupJob
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<RefreshTokenCleanupJob> _logger;

    public RefreshTokenCleanupJob(ApplicationDbContext dbContext, ILogger<RefreshTokenCleanupJob> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Starting RefreshTokenCleanupJob");

        var thresholdDate = DateTimeOffset.UtcNow.AddDays(-7);

        var deletedCount = await _dbContext.Set<docDOC.Domain.Entities.RefreshToken>()
            .Where(rt => (rt.IsRevoked || rt.ExpiresAt < DateTimeOffset.UtcNow) && rt.CreatedAt < thresholdDate)
            .ExecuteDeleteAsync();

        _logger.LogInformation("RefreshTokenCleanupJob finished. Deleted {DeletedCount} obsolete tokens.", deletedCount);
    }
}
