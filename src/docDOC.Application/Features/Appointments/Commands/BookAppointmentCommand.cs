using System.Data.Common;
using docDOC.Application.Interfaces;
using docDOC.Domain.Entities;
using docDOC.Domain.Enums;
using docDOC.Domain.Exceptions;
using docDOC.Domain.Interfaces;
using MediatR;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace docDOC.Application.Features.Appointments.Commands;

public sealed record BookAppointmentCommand(
    int DoctorId,
    DateOnly Date,
    TimeOnly Time,
    AppointmentType Type) : IRequest<BookAppointmentResponse>;

public sealed class BookAppointmentCommandHandler : IRequestHandler<BookAppointmentCommand, BookAppointmentResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IJobScheduler _jobScheduler;
    private readonly ILogger<BookAppointmentCommandHandler> _logger;

    public BookAppointmentCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IJobScheduler jobScheduler,
        ILogger<BookAppointmentCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _jobScheduler = jobScheduler;
        _logger = logger;
    }

    public async Task<BookAppointmentResponse> Handle(BookAppointmentCommand request, CancellationToken cancellationToken)
    {
        
        if (request.Date.DayOfWeek == DayOfWeek.Sunday)
        {
            _logger.LogWarning("Booking failed: Sunday is not allowed.");
            throw new DomainException("Appointments cannot be booked on Sundays.");
        }

if (request.Time.Minute != 0 && request.Time.Minute != 30)
        {
            _logger.LogWarning("Booking failed: Time must be on the hour or half-hour.");
            throw new DomainException("Time must be on the :00 or :30 minute mark.");
        }

var startTime = new TimeOnly(8, 0);
        var endTime = new TimeOnly(16, 30);
        if (request.Time < startTime || request.Time > endTime)
        {
            _logger.LogWarning("Booking failed: Time is outside working hours (08:00 - 16:30).");
            throw new DomainException("Time is outside working hours (08:00 - 16:30).");
        }

        var patientId = _currentUserService.UserId;

await _unitOfWork.BeginTransactionAsync(cancellationToken);
        
        try
        {
            
            bool isTaken = await _unitOfWork.Appointments.IsSlotTakenAsync(request.DoctorId, request.Date, request.Time, cancellationToken);
            if (isTaken)
            {
                _logger.LogWarning("Booking failed: Slot is already taken.");
                throw new ConflictException("The selected time slot is already taken.");
            }

            var appointment = new Appointment
            {
                PatientId = patientId,
                DoctorId = request.DoctorId,
                Date = request.Date,
                Time = request.Time,
                Type = request.Type,
                Status = AppointmentStatus.Pending
            };

            await _unitOfWork.Appointments.AddAsync(appointment, cancellationToken);

try
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx && 
                                             (sqlEx.Number == 2601 || sqlEx.Number == 2627))
            {
                _logger.LogWarning(ex, "Booking failed: Database unique constraint hit for slot.");
                throw new ConflictException("The selected time slot is already taken.");
            }

var appointmentDateTime = request.Date.ToDateTime(request.Time);
            var triggerTime = new DateTimeOffset(appointmentDateTime, TimeSpan.Zero).AddHours(-24);

var jobId = _jobScheduler.ScheduleAppointmentReminder(appointment.Id, triggerTime);
            appointment.HangfireJobId = jobId;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Successfully booked appointment {AppointmentId}", appointment.Id);

            return new BookAppointmentResponse(
                appointment.Id, appointment.DoctorId, appointment.Date,
                appointment.Time, appointment.Type, appointment.Status, appointment.CreatedAt);
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}

public sealed record BookAppointmentResponse(
    int Id, int DoctorId, DateOnly Date, TimeOnly Time,
    AppointmentType Type, AppointmentStatus Status, DateTimeOffset CreatedAt);
