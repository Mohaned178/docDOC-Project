using docDOC.Domain.Entities;

namespace docDOC.Domain.Interfaces;

public interface IReviewRepository : IRepository<Review>
{
    Task<bool> ExistsForAppointmentAsync(int appointmentId, CancellationToken cancellationToken = default);
}

