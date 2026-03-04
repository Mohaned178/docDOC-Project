using docDOC.Application.Interfaces;
using docDOC.Domain.Enums;
using docDOC.Domain.Exceptions;
using docDOC.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace docDOC.Application.Features.Appointments.Commands;

public sealed record UpdateAppointmentStatusCommand(int Id, AppointmentStatus Status) : IRequest<UpdateAppointmentStatusResponse>;

public sealed class UpdateAppointmentStatusCommandHandler : IRequestHandler<UpdateAppointmentStatusCommand, UpdateAppointmentStatusResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly IJobScheduler _jobScheduler;
    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly ILogger<UpdateAppointmentStatusCommandHandler> _logger;

    public UpdateAppointmentStatusCommandHandler(
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUserService,
        IJobScheduler jobScheduler,
        INotificationDispatcher notificationDispatcher,
        ILogger<UpdateAppointmentStatusCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _currentUserService = currentUserService;
        _jobScheduler = jobScheduler;
        _notificationDispatcher = notificationDispatcher;
        _logger = logger;
    }

    public async Task<UpdateAppointmentStatusResponse> Handle(UpdateAppointmentStatusCommand request, CancellationToken cancellationToken)
    {
        var appointment = await _unitOfWork.Appointments.GetByIdAsync(request.Id, cancellationToken);
        if (appointment == null)
        {
            _logger.LogWarning("Appointment {Id} not found.", request.Id);
            throw new NotFoundException($"Appointment {request.Id} not found.");
        }

        var userId = _currentUserService.UserId;
        if (userId == 0) throw new ForbiddenException("User must be authenticated.");
        var userTypeStr = _currentUserService.UserType;

        if (userTypeStr == "Patient" && appointment.PatientId != userId)
        {
            _logger.LogWarning("Patient {UserId} attempted to modify Appointment {Id} which belongs to Patient {PatientId}", userId, request.Id, appointment.PatientId);
            throw new ForbiddenException("You do not have access to this appointment.");
        }

        if (userTypeStr == "Doctor" && appointment.DoctorId != userId)
        {
            _logger.LogWarning("Doctor {UserId} attempted to modify Appointment {Id} which belongs to Doctor {DoctorId}", userId, request.Id, appointment.DoctorId);
            throw new ForbiddenException("You do not have access to this appointment.");
        }

        if (appointment.Status == AppointmentStatus.Completed)
        {
            _logger.LogWarning("Attempted to change status of completed appointment {Id}.", request.Id);
            throw new ConflictException("Cannot change status of a completed appointment.");
        }

        if (appointment.Status == AppointmentStatus.Cancelled)
        {
            _logger.LogWarning("Attempted to change status of cancelled appointment {Id}.", request.Id);
            throw new ConflictException("Cannot change status of a cancelled appointment.");
        }

        if (appointment.Status == AppointmentStatus.Pending)
        {
            if (request.Status == AppointmentStatus.Confirmed)
            {
                if (userTypeStr != "Doctor") throw new ForbiddenException("Only doctors can confirm appointments.");
                
                appointment.Status = AppointmentStatus.Confirmed;
                _unitOfWork.Appointments.Update(appointment);

                await _notificationDispatcher.SendAsync(
                    appointment.PatientId,
                    UserType.Patient,
                    "appointment_confirmed",
                    $"Your appointment on {appointment.Date} has been confirmed.",
                    appointment.Id);
            }
            else if (request.Status == AppointmentStatus.Cancelled)
            {
                await HandleCancellation(appointment, userTypeStr, cancellationToken);
            }
            else
            {
                throw new DomainException("Invalid status transition from Pending.");
            }
        }
        else if (appointment.Status == AppointmentStatus.Confirmed)
        {
            if (request.Status == AppointmentStatus.Completed)
            {
                if (userTypeStr != "Doctor") throw new ForbiddenException("Only doctors can complete appointments.");
                
                appointment.Status = AppointmentStatus.Completed;
                _unitOfWork.Appointments.Update(appointment);

                await _notificationDispatcher.SendAsync(
                    appointment.PatientId,
                    UserType.Patient,
                    "appointment_completed",
                    $"Your appointment on {appointment.Date} has been completed. You can now leave a review.",
                    appointment.Id);
            }
            else if (request.Status == AppointmentStatus.Cancelled)
            {
                await HandleCancellation(appointment, userTypeStr, cancellationToken);
            }
            else
            {
                throw new DomainException("Invalid status transition from Confirmed.");
            }
        }
        else
        {
            throw new DomainException("Invalid status transition.");
        }
        
        _logger.LogInformation("Successfully updated appointment {Id} to {Status}", request.Id, request.Status);

        return new UpdateAppointmentStatusResponse(appointment.Id, appointment.Status);
    }

    private async Task HandleCancellation(docDOC.Domain.Entities.Appointment appointment, string userTypeStr, CancellationToken cancellationToken)
    {
        appointment.Status = AppointmentStatus.Cancelled;
        _unitOfWork.Appointments.Update(appointment);

        if (!string.IsNullOrEmpty(appointment.HangfireJobId))
        {
            _jobScheduler.DeleteJob(appointment.HangfireJobId);
            appointment.HangfireJobId = null;
        }

        int notifyId = userTypeStr == "Patient" ? appointment.DoctorId : appointment.PatientId;
        UserType notifyType = userTypeStr == "Patient" ? UserType.Doctor : UserType.Patient;
        
        await _notificationDispatcher.SendAsync(
            notifyId,
            notifyType,
            "appointment_cancelled",
            $"The appointment on {appointment.Date} has been cancelled.",
            appointment.Id);

        var chatRoom = await _unitOfWork.ChatRooms.GetByPairAsync(appointment.PatientId, appointment.DoctorId, cancellationToken);
        if (chatRoom != null && chatRoom.IsActive)
        {
            bool hasQualifying = await _unitOfWork.ChatRooms.HasQualifyingAppointmentAsync(appointment.PatientId, appointment.DoctorId, cancellationToken);
            if (!hasQualifying)
            {
                chatRoom.IsActive = false;
                _unitOfWork.ChatRooms.Update(chatRoom);
                _logger.LogInformation("Chat room between Patient {PatientId} and Doctor {DoctorId} has been soft-closed due to cancellation.", appointment.PatientId, appointment.DoctorId);
            }
        }
    }
}

public sealed record UpdateAppointmentStatusResponse(int Id, AppointmentStatus Status);
