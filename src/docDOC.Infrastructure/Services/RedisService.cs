using StackExchange.Redis;
using docDOC.Application.Interfaces;

namespace docDOC.Infrastructure.Services;

public class RedisService : IRedisService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;

    public RedisService(IConnectionMultiplexer redis)
    {
        _redis = redis;
        _db = _redis.GetDatabase();
    }

    public async Task SetAsync(string key, string value, TimeSpan expiration)
    {
        await _db.StringSetAsync(key, value, expiration);
    }

    public async Task<string?> GetAsync(string key)
    {
        return await _db.StringGetAsync(key);
    }

    public async Task RemoveAsync(string key)
    {
        await _db.KeyDeleteAsync(key);
    }

    public async Task<bool> ExistsAsync(string key)
    {
        return await _db.KeyExistsAsync(key);
    }

    public async Task GeoAddAsync(string key, double longitude, double latitude, string member)
    {
        await _db.GeoAddAsync(key, longitude, latitude, member);
    }

    public async Task GeoRemoveAsync(string key, string member)
    {
        await _db.GeoRemoveAsync(key, member);
    }

    public async Task<string[]> GeoSearchAsync(string key, double longitude, double latitude, double radiusKm, int count = 20)
    {
        var results = await _db.GeoRadiusAsync(key, longitude, latitude, radiusKm, GeoUnit.Kilometers, count);
        return results.Select(r => r.Member.ToString()).ToArray();
    }

    public async Task<long> IncrementAsync(string key)
    {
        return await _db.StringIncrementAsync(key);
    }
}
