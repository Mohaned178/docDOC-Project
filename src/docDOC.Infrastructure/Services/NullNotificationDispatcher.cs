using docDOC.Application.Interfaces;
using docDOC.Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace docDOC.Infrastructure.Services;

public class NullNotificationDispatcher : INotificationDispatcher
{
    private readonly ILogger<NullNotificationDispatcher> _logger;

    public NullNotificationDispatcher(ILogger<NullNotificationDispatcher> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(int userId, UserType userType, string eventType, string content, int? referenceId = null)
    {
        _logger.LogInformation("NullNotificationDispatcher intercepted SendAsync for User {UserId} ({UserType}), Event {EventType}", userId, userType, eventType);
        return Task.CompletedTask;
    }
}
