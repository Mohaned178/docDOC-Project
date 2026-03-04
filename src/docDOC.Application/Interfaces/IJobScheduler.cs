namespace docDOC.Application.Interfaces;

public interface IJobScheduler
{
    string ScheduleAppointmentReminder(int appointmentId, DateTimeOffset triggerTime);
    void DeleteJob(string jobId);
}
