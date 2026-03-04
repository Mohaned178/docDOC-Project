namespace docDOC.Domain.Entities;

public class Speciality : BaseEntity
{
    public required string Name { get; set; }
    public string? IconCode { get; set; }
}
