using docDOC.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace docDOC.Infrastructure.Persistence.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments");
        builder.HasKey(a => a.Id);
        
        builder.Property(a => a.Type).HasConversion<string>().IsRequired();
        builder.Property(a => a.Status).HasConversion<string>().IsRequired();
        builder.Property(a => a.HangfireJobId).HasMaxLength(100);
        
        builder.HasOne(a => a.Patient)
               .WithMany()
               .HasForeignKey(a => a.PatientId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Doctor)
               .WithMany()
               .HasForeignKey(a => a.DoctorId)
               .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasIndex(a => new { a.DoctorId, a.Date, a.Time })
               .HasFilter("[Status] != 'Cancelled'")
               .IsUnique();
               
        builder.HasIndex(a => a.PatientId);
        builder.HasIndex(a => a.DoctorId);
        builder.HasIndex(a => new { a.PatientId, a.Status });
        builder.HasIndex(a => new { a.DoctorId, a.Date, a.Status });
    }
}
