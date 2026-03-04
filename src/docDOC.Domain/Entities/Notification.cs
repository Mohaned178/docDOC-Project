using docDOC.Domain.Enums;

namespace docDOC.Domain.Entities;

public class Notification : BaseEntity
{
    public int UserId { get; set; }
    public UserType UserType { get; set; }
    public required string EventType { get; set; }
    public required string Content { get; set; }
    public int? ReferenceId { get; set; }
    public bool IsRead { get; set; } = false;
}
