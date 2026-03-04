using docDOC.Domain.Entities;
using docDOC.Domain.Enums;

namespace docDOC.Domain.Interfaces;

public interface IAppointmentRepository : IRepository<Appointment>
{
    Task<bool> IsSlotTakenAsync(int doctorId, DateOnly date, TimeOnly time, CancellationToken cancellationToken = default);
    Task<IEnumerable<Appointment>> GetByPairAsync(int patientId, int doctorId, CancellationToken cancellationToken = default);
    Task<(IEnumerable<Appointment> Items, int TotalCount)> GetPagedForUserAsync(int userId, string userType, AppointmentStatus? status, int page, int pageSize, CancellationToken cancellationToken = default);
}
