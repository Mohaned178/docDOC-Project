using docDOC.Domain.Entities;

namespace docDOC.Domain.Interfaces;

public interface IMessageRepository : IRepository<Message>
{
    Task<IEnumerable<Message>> GetPagedAsync(int roomId, DateTimeOffset cursor, int limit, CancellationToken cancellationToken = default);
    Task<IEnumerable<Message>> GetPagedByCursorAsync(int roomId, int? cursorId, int limit, CancellationToken cancellationToken = default);
    Task MarkAsReadAsync(int roomId, int recipientId, CancellationToken cancellationToken = default);
}
