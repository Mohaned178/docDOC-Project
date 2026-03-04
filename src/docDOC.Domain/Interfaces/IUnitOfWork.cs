using docDOC.Domain.Entities;

namespace docDOC.Domain.Interfaces;

public interface IUnitOfWork
{
    IPatientRepository Patients { get; }
    IDoctorRepository Doctors { get; }
    ISpecialityRepository Specialities { get; }
    IAppointmentRepository Appointments { get; }
    IChatRoomRepository ChatRooms { get; }
    IMessageRepository Messages { get; }
    INotificationRepository Notifications { get; }
    IReviewRepository Reviews { get; }
    IRefreshTokenRepository RefreshTokens { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
