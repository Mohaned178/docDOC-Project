namespace docDOC.Domain.Entities;

public class Review : BaseEntity
{
    public int AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }
    public int PatientId { get; set; }
    public Patient? Patient { get; set; }
    public int DoctorId { get; set; }
    public Doctor? Doctor { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
}
