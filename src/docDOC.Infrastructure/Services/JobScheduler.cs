using docDOC.Application.Interfaces;
using docDOC.Infrastructure.Services.HangfireJobs;
using Hangfire;

namespace docDOC.Infrastructure.Services;

public class JobScheduler : IJobScheduler
{
    public string ScheduleAppointmentReminder(int appointmentId, DateTimeOffset triggerTime)
    {
        return BackgroundJob.Schedule<AppointmentReminderJob>(j => j.Execute(appointmentId), triggerTime);
    }

    public void DeleteJob(string jobId)
    {
        BackgroundJob.Delete(jobId);
    }
}
