using docDOC.Domain.Entities;
using docDOC.Domain.Enums;
using docDOC.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace docDOC.Infrastructure.Persistence.Repositories;

public class AppointmentRepository : BaseRepository<Appointment>, IAppointmentRepository
{
    public AppointmentRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<bool> IsSlotTakenAsync(int doctorId, DateOnly date, TimeOnly time, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(a => 
            a.DoctorId == doctorId && 
            a.Date == date && 
            a.Time == time && 
            a.Status != AppointmentStatus.Cancelled, 
            cancellationToken);
    }

    public async Task<IEnumerable<Appointment>> GetByPairAsync(int patientId, int doctorId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(a => a.PatientId == patientId && a.DoctorId == doctorId)
                           .ToListAsync(cancellationToken);
    }

    public async Task<(IEnumerable<Appointment> Items, int TotalCount)> GetPagedForUserAsync(
        int userId, string userType, AppointmentStatus? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = userType == "Doctor"
            ? _dbSet.Where(a => a.DoctorId == userId)
            : _dbSet.Where(a => a.PatientId == userId);

        if (status.HasValue)
            query = query.Where(a => a.Status == status.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(a => a.Date).ThenByDescending(a => a.Time)
                               .Skip((page - 1) * pageSize)
                               .Take(pageSize)
                               .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
