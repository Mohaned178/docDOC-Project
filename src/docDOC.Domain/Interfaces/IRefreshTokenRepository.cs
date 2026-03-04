using docDOC.Domain.Entities;

namespace docDOC.Domain.Interfaces;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> GetByHashAsync(string hash, CancellationToken cancellationToken = default);
    Task RevokeAllForUserAsync(int userId, CancellationToken cancellationToken = default);
}
