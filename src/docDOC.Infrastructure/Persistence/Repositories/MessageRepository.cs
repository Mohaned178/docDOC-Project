using docDOC.Domain.Entities;
using docDOC.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace docDOC.Infrastructure.Persistence.Repositories;

public class MessageRepository : BaseRepository<Message>, IMessageRepository
{
    public MessageRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Message>> GetPagedAsync(int roomId, DateTimeOffset cursor, int limit, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(m => m.ChatRoomId == roomId && m.SentAt < cursor)
                           .OrderByDescending(m => m.SentAt)
                           .Take(limit)
                           .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Message>> GetPagedByCursorAsync(int roomId, int? cursorId, int limit, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(m => m.ChatRoomId == roomId);

        if (cursorId.HasValue)
        {
            var cursorMsg = await _dbSet.FirstOrDefaultAsync(m => m.Id == cursorId.Value, cancellationToken);
            if (cursorMsg != null)
            {
                query = query.Where(m => m.SentAt < cursorMsg.SentAt || (m.SentAt == cursorMsg.SentAt && m.Id != cursorMsg.Id));
            }
        }

        return await query.OrderByDescending(m => m.SentAt)
                          .Take(limit)
                          .ToListAsync(cancellationToken);
    }

    public async Task MarkAsReadAsync(int roomId, int recipientId, CancellationToken cancellationToken = default)
    {

await _dbSet.Where(m => m.ChatRoomId == roomId && m.SenderId != recipientId && m.Status != docDOC.Domain.Enums.MessageStatus.Read)
                    .ExecuteUpdateAsync(s => s.SetProperty(m => m.Status, docDOC.Domain.Enums.MessageStatus.Read), cancellationToken);
    }
}
