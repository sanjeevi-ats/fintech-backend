using System;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace Fintech.Application.Services;

public interface IRedisLockService
{
    Task<bool> AcquireLockAsync(string key, TimeSpan expiry);
    Task ReleaseLockAsync(string key);
}

public class RedisLockService : IRedisLockService
{
    private readonly IConnectionMultiplexer _redis;

    public RedisLockService(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<bool> AcquireLockAsync(string key, TimeSpan expiry)
    {
        try
        {
            // Check if Redis is connected
            if (_redis == null || !_redis.IsConnected)
            {
                System.Diagnostics.Debug.WriteLine("Redis not connected, skipping lock");
                return true; // Allow operation to proceed without lock
            }

            var db = _redis.GetDatabase();
            return await db.StringSetAsync(key, "locked", expiry, When.NotExists);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Redis lock error: {ex.Message}. Allowing operation to proceed.");
            return true; // Allow operation to proceed if Redis fails
        }
    }

    public async Task ReleaseLockAsync(string key)
    {
        try
        {
            // Check if Redis is connected
            if (_redis == null || !_redis.IsConnected)
            {
                System.Diagnostics.Debug.WriteLine("Redis not connected, skipping lock release");
                return;
            }

            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Redis release error: {ex.Message}. Continuing...");
            // Don't throw, just log and continue
        }
    }
}
