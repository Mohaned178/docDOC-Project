using docDOC.Domain.Entities;

namespace docDOC.Domain.Interfaces;

public interface IDoctorRepository : IRepository<Doctor>
{
    Task<IEnumerable<Doctor>> GetNearbyAsync(double lat, double lon, double radiusKm, int? specialityId, CancellationToken cancellationToken = default);
    Task<Doctor?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
}
