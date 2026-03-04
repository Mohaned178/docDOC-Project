using docDOC.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace docDOC.Infrastructure.Persistence.Configurations;

public class SpecialityConfiguration : IEntityTypeConfiguration<Speciality>
{
    public void Configure(EntityTypeBuilder<Speciality> builder)
    {
        builder.ToTable("Specialities");
        builder.HasKey(s => s.Id);
        
        builder.Property(s => s.Name).HasMaxLength(100).IsRequired();
        builder.Property(s => s.IconCode).HasMaxLength(50);
        
        builder.HasIndex(s => s.Name).IsUnique();

        var date = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        builder.HasData(
            new Speciality { Id = 1, Name = "General",    IconCode = "general", CreatedAt = date },
            new Speciality { Id = 2, Name = "Neurologic", IconCode = "neurologic", CreatedAt = date },
            new Speciality { Id = 3, Name = "Pediatric",  IconCode = "pediatric", CreatedAt = date },
            new Speciality { Id = 4, Name = "Radiology",  IconCode = "radiology", CreatedAt = date }
        );
    }
}
