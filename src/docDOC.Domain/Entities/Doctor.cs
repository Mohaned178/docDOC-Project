using docDOC.Domain.Enums;
using NetTopologySuite.Geometries;

namespace docDOC.Domain.Entities;

public class Doctor : BaseEntity
{
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public int SpecialityId { get; set; }
    public Speciality? Speciality { get; set; }
    public string? Hospital { get; set; }
    public decimal AverageRating { get; set; } = 0.00m;
    public int TotalReviews { get; set; } = 0;
    public bool IsOnline { get; set; } = false;
    public Point? Location { get; set; }
}
