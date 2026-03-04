using docDOC.Domain.Enums;

namespace docDOC.Domain.Entities;

public class Appointment : BaseEntity
{
    public int PatientId { get; set; }
    public Patient? Patient { get; set; }
    public int DoctorId { get; set; }
    public Doctor? Doctor { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly Time { get; set; }
    public AppointmentType Type { get; set; }
    public AppointmentStatus Status { get; set; }
    public string? HangfireJobId { get; set; }
}
