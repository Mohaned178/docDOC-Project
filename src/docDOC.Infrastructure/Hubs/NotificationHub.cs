using docDOC.Application.Interfaces;
using docDOC.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace docDOC.Infrastructure.Hubs;

[Authorize]
public sealed class NotificationHub : Hub
{
    private readonly IRedisService _redisService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<NotificationHub> _logger;

    public NotificationHub(
        IRedisService redisService,
        ICurrentUserService currentUserService,
        ILogger<NotificationHub> logger)
    {
        _redisService = redisService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = _currentUserService.UserId;
        if (userId != 0)
        {
            var key = $"online:{userId}";
            await _redisService.SetAsync(key, "true", TimeSpan.FromSeconds(30));
            await Groups.AddToGroupAsync(Context.ConnectionId, userId.ToString());
            
            _logger.LogInformation("User {UserId} connected and marked online", userId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = _currentUserService.UserId;
        if (userId != 0)
        {
            await _redisService.RemoveAsync($"online:{userId}");
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId.ToString());
            
            _logger.LogInformation("User {UserId} disconnected and marked offline", userId);
        }

        await base.OnDisconnectedAsync(exception);
    }

public async Task Ping()
    {
        var userId = _currentUserService.UserId;
        if (userId != 0)
        {
            await _redisService.SetAsync($"online:{userId}", "true", TimeSpan.FromSeconds(30));
        }
    }
}
