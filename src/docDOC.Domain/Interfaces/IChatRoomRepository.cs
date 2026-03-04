using docDOC.Domain.Entities;

namespace docDOC.Domain.Interfaces;

public interface IChatRoomRepository : IRepository<ChatRoom>
{
    Task<ChatRoom?> GetByPairAsync(int patientId, int doctorId, CancellationToken cancellationToken = default);
    Task<bool> HasQualifyingAppointmentAsync(int patientId, int doctorId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChatRoom>> GetMyRoomsAsync(int userId, CancellationToken cancellationToken = default);
}
