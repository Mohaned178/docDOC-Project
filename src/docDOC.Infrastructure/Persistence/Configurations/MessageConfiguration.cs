using docDOC.Domain.Entities;
using docDOC.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace docDOC.Infrastructure.Persistence.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("Messages");
        builder.HasKey(m => m.Id);
        
        builder.Property(m => m.Content).HasMaxLength(2000).IsRequired();
        builder.Property(m => m.SenderType).HasConversion<string>().IsRequired();
        builder.Property(m => m.Status).HasConversion<string>().HasDefaultValue(MessageStatus.Sent);
        
        builder.HasOne(m => m.ChatRoom)
               .WithMany()
               .HasForeignKey(m => m.ChatRoomId)
               .OnDelete(DeleteBehavior.Cascade);
               
        builder.HasIndex(m => new { m.ChatRoomId, m.SentAt });
        builder.HasIndex(m => new { m.ChatRoomId, m.Status })
               .HasFilter("[Status] != 'Read'");
    }
}
