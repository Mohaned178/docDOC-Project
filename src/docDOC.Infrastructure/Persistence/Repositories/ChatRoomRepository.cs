using docDOC.Domain.Entities;
using docDOC.Domain.Enums;
using docDOC.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace docDOC.Infrastructure.Persistence.Repositories;

public class ChatRoomRepository : BaseRepository<ChatRoom>, IChatRoomRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ChatRoomRepository(ApplicationDbContext context) : base(context)
    {
        _dbContext = context;
    }

    public async Task<ChatRoom?> GetByPairAsync(int patientId, int doctorId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(c => c.PatientId == patientId && c.DoctorId == doctorId, cancellationToken);
    }

    public async Task<bool> HasQualifyingAppointmentAsync(int patientId, int doctorId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Appointments.AnyAsync(a => 
            a.PatientId == patientId && 
            a.DoctorId == doctorId && 
            (a.Status == AppointmentStatus.Confirmed || a.Status == AppointmentStatus.Completed), 
            cancellationToken);
    }

    public async Task<IEnumerable<ChatRoom>> GetMyRoomsAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.Patient)
            .Include(c => c.Doctor)
            .Where(c => c.PatientId == userId || c.DoctorId == userId)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync(cancellationToken);
    }
}
