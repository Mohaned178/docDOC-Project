using docDOC.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace docDOC.Infrastructure.Persistence.Configurations;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("Patients");
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.Email).HasMaxLength(100).IsRequired();
        builder.Property(p => p.PasswordHash).HasMaxLength(200).IsRequired();
        builder.Property(p => p.FirstName).HasMaxLength(50).IsRequired();
        builder.Property(p => p.LastName).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Gender).HasConversion<string>();
        
        builder.HasIndex(p => p.Email).IsUnique();
    }
}
