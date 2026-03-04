using docDOC.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace docDOC.Infrastructure.Persistence.Configurations;

public class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> builder)
    {
        builder.ToTable("Doctors");
        builder.HasKey(d => d.Id);
        
        builder.Property(d => d.Email).HasMaxLength(100).IsRequired();
        builder.Property(d => d.PasswordHash).HasMaxLength(200).IsRequired();
        builder.Property(d => d.FirstName).HasMaxLength(50).IsRequired();
        builder.Property(d => d.LastName).HasMaxLength(50).IsRequired();
        builder.Property(d => d.Hospital).HasMaxLength(100);
        builder.Property(d => d.AverageRating).HasPrecision(3, 2).HasDefaultValue(0.00m);
        builder.Property(d => d.TotalReviews).HasDefaultValue(0);
        builder.Property(d => d.IsOnline).HasDefaultValue(false);
        builder.Property(d => d.Location).HasColumnType("geometry");
        
        builder.HasOne(d => d.Speciality)
               .WithMany()
               .HasForeignKey(d => d.SpecialityId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => d.Email).IsUnique();
        builder.HasIndex(d => d.SpecialityId);
        builder.HasIndex(d => d.IsOnline).HasFilter("[IsOnline] = 1");
    }
}
