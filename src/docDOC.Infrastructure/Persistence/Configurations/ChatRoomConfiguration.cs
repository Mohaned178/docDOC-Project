using docDOC.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace docDOC.Infrastructure.Persistence.Configurations;

public class ChatRoomConfiguration : IEntityTypeConfiguration<ChatRoom>
{
    public void Configure(EntityTypeBuilder<ChatRoom> builder)
    {
        builder.ToTable("ChatRooms");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.IsActive).HasDefaultValue(true);
        
        builder.HasOne(c => c.Patient)
               .WithMany()
               .HasForeignKey(c => c.PatientId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.Doctor)
               .WithMany()
               .HasForeignKey(c => c.DoctorId)
               .OnDelete(DeleteBehavior.Restrict);
               
        builder.HasIndex(c => new { c.PatientId, c.DoctorId }).IsUnique();
        builder.HasIndex(c => c.UpdatedAt);
    }
}
