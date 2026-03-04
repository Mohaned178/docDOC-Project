using docDOC.Domain.Entities;
using docDOC.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace docDOC.Infrastructure.Persistence.Repositories;

public class RefreshTokenRepository : BaseRepository<RefreshToken>, IRefreshTokenRepository
{
    public RefreshTokenRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<RefreshToken?> GetByHashAsync(string hash, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(rt => rt.TokenHash == hash, cancellationToken);
    }

    public async Task RevokeAllForUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var tokens = await _dbSet.Where(rt => rt.UserId == userId && !rt.IsRevoked)
                                 .ToListAsync(cancellationToken);
                                 
        foreach (var token in tokens)
        {
            token.IsRevoked = true;
        }
    }
}
