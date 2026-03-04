using docDOC.Domain.Entities;
using docDOC.Domain.Interfaces;

using Microsoft.EntityFrameworkCore;

namespace docDOC.Infrastructure.Persistence.Repositories;

public class NotificationRepository : BaseRepository<Notification>, INotificationRepository
{
    public NotificationRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Notification>> GetPagedAsync(int userId, bool unreadOnly, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(n => n.UserId == userId);
        
        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        return await query.OrderByDescending(n => n.CreatedAt)
                          .Skip((page - 1) * pageSize)
                          .Take(pageSize)
                          .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalCountAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.CountAsync(n => n.UserId == userId, cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);
    }

    public async Task MarkAllAsReadAsync(int userId, CancellationToken cancellationToken = default)
    {
        await _dbSet.Where(n => n.UserId == userId && !n.IsRead)
                    .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), cancellationToken);
    }
}
