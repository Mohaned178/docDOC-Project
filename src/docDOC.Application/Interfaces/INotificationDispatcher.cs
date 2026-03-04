using docDOC.Domain.Enums;

namespace docDOC.Application.Interfaces;

public interface INotificationDispatcher
{
    Task SendAsync(int userId, UserType userType, string eventType, string content, int? referenceId = null);
}
