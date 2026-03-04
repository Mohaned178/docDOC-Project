using docDOC.Domain.Entities;
using docDOC.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace docDOC.Infrastructure.Persistence.Repositories;

public class DoctorRepository : BaseRepository<Doctor>, IDoctorRepository
{
    public DoctorRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Doctor>> GetNearbyAsync(double lat, double lon, double radiusKm, int? specialityId, CancellationToken cancellationToken = default)
    {
        var location = new Point(lon, lat) { SRID = 4326 };
        var radiusMeters = radiusKm * 1000;

        var query = _dbSet.Where(d => d.IsOnline && d.Location != null && d.Location.Distance(location) <= radiusMeters);

        if (specialityId.HasValue)
        {
            query = query.Where(d => d.SpecialityId == specialityId.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<Doctor?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(d => d.Email == email, cancellationToken);
    }
}
