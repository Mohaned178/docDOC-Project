using docDOC.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace docDOC.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(n => n.Id);
        
        builder.Property(n => n.EventType).HasMaxLength(50).IsRequired();
        builder.Property(n => n.Content).HasMaxLength(500).IsRequired();
        builder.Property(n => n.UserType).HasConversion<string>().IsRequired();
        builder.Property(n => n.IsRead).HasDefaultValue(false);
        
        builder.HasIndex(n => new { n.UserId, n.UserType })
               .HasFilter("[IsRead] = 0")
               .HasDatabaseName("IX_Notifications_Unread");
               
        builder.HasIndex(n => new { n.UserId, n.CreatedAt });
    }
}
