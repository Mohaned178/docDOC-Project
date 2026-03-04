using docDOC.Domain.Entities;
using docDOC.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace docDOC.Infrastructure.Persistence.Repositories;

public class ReviewRepository : BaseRepository<Review>, IReviewRepository
{
    public ReviewRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<bool> ExistsForAppointmentAsync(int appointmentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(r => r.AppointmentId == appointmentId, cancellationToken);
    }
}

