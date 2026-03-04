using docDOC.Domain.Enums;

namespace docDOC.Domain.Entities;

public class Message : BaseEntity
{
    public int ChatRoomId { get; set; }
    public ChatRoom? ChatRoom { get; set; }
    public int SenderId { get; set; }
    public SenderType SenderType { get; set; }
    public required string Content { get; set; }
    public MessageStatus Status { get; set; } = MessageStatus.Sent;
    public DateTimeOffset SentAt { get; set; } = DateTimeOffset.UtcNow;
}
