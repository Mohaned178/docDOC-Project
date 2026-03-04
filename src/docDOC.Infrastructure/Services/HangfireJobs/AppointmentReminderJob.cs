using docDOC.Application.Interfaces;
using docDOC.Domain.Enums;
using docDOC.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace docDOC.Infrastructure.Services.HangfireJobs;

public class AppointmentReminderJob
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly INotificationDispatcher _notificationDispatcher;
    private readonly ILogger<AppointmentReminderJob> _logger;

    public AppointmentReminderJob(
        IUnitOfWork unitOfWork,
        INotificationDispatcher notificationDispatcher,
        ILogger<AppointmentReminderJob> logger)
    {
        _unitOfWork = unitOfWork;
        _notificationDispatcher = notificationDispatcher;
        _logger = logger;
    }

    public async Task Execute(int appointmentId)
    {
        _logger.LogInformation("Executing AppointmentReminderJob for AppointmentId: {AppointmentId}", appointmentId);

        var appointment = await _unitOfWork.Appointments.GetByIdAsync(appointmentId);

        if (appointment == null)
        {
            _logger.LogWarning("Appointment {AppointmentId} not found, reminder aborted.", appointmentId);
            return;
        }

        if (appointment.Status != AppointmentStatus.Confirmed)
        {
            _logger.LogInformation("Appointment {AppointmentId} is not confirmed (Status: {Status}), reminder skipped.", appointmentId, appointment.Status);
            return;
        }

        await _notificationDispatcher.SendAsync(
            appointment.PatientId,
            UserType.Patient,
            "appointment_reminder",
            $"Reminder: Your appointment is scheduled for {appointment.Date} at {appointment.Time}.",
            appointment.Id);
        
        await _notificationDispatcher.SendAsync(
            appointment.DoctorId,
            UserType.Doctor,
            "appointment_reminder",
            $"Reminder: You have an appointment scheduled for {appointment.Date} at {appointment.Time}.",
            appointment.Id);

        _logger.LogInformation("Reminders sent for AppointmentId: {AppointmentId}", appointmentId);
    }
}
