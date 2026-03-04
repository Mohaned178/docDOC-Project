using docDOC.Domain.Enums;

namespace docDOC.Domain.Entities;

public class Patient : BaseEntity
{
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public DateOnly DateOfBirth { get; set; }
    public Gender Gender { get; set; }
}
