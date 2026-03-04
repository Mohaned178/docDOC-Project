using docDOC.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace docDOC.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(r => r.Id);
        
        builder.Property(r => r.TokenHash).HasMaxLength(256).IsRequired();
        builder.Property(r => r.UserType).HasConversion<string>().IsRequired();
        builder.Property(r => r.IsRevoked).HasDefaultValue(false);
        
        builder.HasIndex(r => r.TokenHash).IsUnique();
        builder.HasIndex(r => new { r.UserId, r.UserType });
        builder.HasIndex(r => r.ExpiresAt);
    }
}
