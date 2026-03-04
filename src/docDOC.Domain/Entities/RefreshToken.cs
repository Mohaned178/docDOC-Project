using docDOC.Domain.Enums;

namespace docDOC.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public int UserId { get; set; }
    public UserType UserType { get; set; }
    public required string TokenHash { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public bool IsRevoked { get; set; } = false;
}
