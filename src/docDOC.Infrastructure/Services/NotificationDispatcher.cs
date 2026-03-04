using docDOC.Application.Interfaces;
using docDOC.Domain.Entities;
using docDOC.Domain.Enums;
using docDOC.Domain.Interfaces;
using docDOC.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace docDOC.Infrastructure.Services;

public sealed class NotificationDispatcher : INotificationDispatcher
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRedisService _redisService;
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<NotificationDispatcher> _logger;

    public NotificationDispatcher(
        IUnitOfWork unitOfWork,
        IRedisService redisService,
        IHubContext<NotificationHub> hubContext,
        ILogger<NotificationDispatcher> logger)
    {
        _unitOfWork = unitOfWork;
        _redisService = redisService;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task SendAsync(int userId, UserType userType, string eventType, string content, int? referenceId = null)
    {
        _logger.LogInformation("Dispatching notification {EventType} to user {UserId}", eventType, userId);

var notification = new Notification
        {
            UserId = userId,
            UserType = userType,
            EventType = eventType,
            Content = content,
            ReferenceId = referenceId,
            CreatedAt = DateTimeOffset.UtcNow,
            IsRead = false
        };

        await _unitOfWork.Notifications.AddAsync(notification);
        await _unitOfWork.SaveChangesAsync(); 

var isOnline = await _redisService.ExistsAsync($"online:{userId}");

if (isOnline)
        {
            await _hubContext.Clients.Group(userId.ToString())
                .SendAsync("OnNotification", new
                {
                    notification.Id,
                    notification.EventType,
                    notification.Content,
                    notification.ReferenceId,
                    notification.CreatedAt
                });
            
            _logger.LogInformation("Notification pushed to SignalR group for user {UserId}", userId);
        }
        else
        {
            _logger.LogInformation("User {UserId} is offline, notification persisted for pull", userId);
        }
    }
}
