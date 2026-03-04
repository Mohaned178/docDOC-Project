namespace docDOC.Application.Interfaces;

public interface IRedisService
{
    Task SetAsync(string key, string value, TimeSpan expiration);
    Task<string?> GetAsync(string key);
    Task RemoveAsync(string key);
    Task<bool> ExistsAsync(string key);
    Task GeoAddAsync(string key, double longitude, double latitude, string member);
    Task GeoRemoveAsync(string key, string member);
    Task<string[]> GeoSearchAsync(string key, double longitude, double latitude, double radiusKm, int count = 20);
    Task<long> IncrementAsync(string key);
}
