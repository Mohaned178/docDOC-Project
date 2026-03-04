using docDOC.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace docDOC.Infrastructure.Persistence.Configurations;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("Reviews");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Comment).HasMaxLength(1000);
        
        builder.HasOne(r => r.Appointment)
               .WithOne()
               .HasForeignKey<Review>(r => r.AppointmentId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Patient)
               .WithMany()
               .HasForeignKey(r => r.PatientId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Doctor)
               .WithMany()
               .HasForeignKey(r => r.DoctorId)
               .OnDelete(DeleteBehavior.Restrict);
               
        builder.HasIndex(r => r.AppointmentId).IsUnique();
        builder.HasIndex(r => new { r.DoctorId, r.CreatedAt });
    }
}
