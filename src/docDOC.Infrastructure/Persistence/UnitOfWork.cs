using docDOC.Domain.Interfaces;

namespace docDOC.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public IPatientRepository Patients { get; }
    public IDoctorRepository Doctors { get; }
    public ISpecialityRepository Specialities { get; }
    public IAppointmentRepository Appointments { get; }
    public IChatRoomRepository ChatRooms { get; }
    public IMessageRepository Messages { get; }
    public INotificationRepository Notifications { get; }
    public IReviewRepository Reviews { get; }
    public IRefreshTokenRepository RefreshTokens { get; }

    public UnitOfWork(
        ApplicationDbContext context,
        IPatientRepository patients,
        IDoctorRepository doctors,
        ISpecialityRepository specialities,
        IAppointmentRepository appointments,
        IChatRoomRepository chatRooms,
        IMessageRepository messages,
        INotificationRepository notifications,
        IReviewRepository reviews,
        IRefreshTokenRepository refreshTokens)
    {
        _context = context;
        Patients = patients;
        Doctors = doctors;
        Specialities = specialities;
        Appointments = appointments;
        ChatRooms = chatRooms;
        Messages = messages;
        Notifications = notifications;
        Reviews = reviews;
        RefreshTokens = refreshTokens;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_context.Database.CurrentTransaction != null)
        {
            await _context.Database.CommitTransactionAsync(cancellationToken);
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_context.Database.CurrentTransaction != null)
        {
            await _context.Database.RollbackTransactionAsync(cancellationToken);
        }
    }
}
