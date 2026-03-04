using docDOC.Domain.Entities;

namespace docDOC.Domain.Interfaces;

public interface INotificationRepository : IRepository<Notification>
{
    Task<IEnumerable<Notification>> GetPagedAsync(int userId, bool unreadOnly, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(int userId, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default);
    Task MarkAllAsReadAsync(int userId, CancellationToken cancellationToken = default);
}
