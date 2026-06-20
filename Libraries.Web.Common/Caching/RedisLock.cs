using StackExchange.Redis;

namespace Libraries.Web.Common.Caching
{
    public class RedisLock(IConnectionMultiplexer redis) : IDistributedLock
    {
        public async Task<IAsyncDisposable?> AcquireAsync(string key, TimeSpan expiry)
        {
            try
            {
                var db = redis.GetDatabase();
                var lockValue = Guid.NewGuid().ToString();
                var acquired = await db.LockTakeAsync(key, lockValue, expiry);
                if (!acquired) return null;
                return new LockReleaser(db, key, lockValue);
            }
            catch (RedisConnectionException)
            {
                return null;
            }
        }

        private class LockReleaser(IDatabase db, string key, string lockValue) : IAsyncDisposable
        {
            public async ValueTask DisposeAsync()
            {
                try { await db.LockReleaseAsync(key, lockValue); }
                catch { }
            }
        }
    }

    public interface IDistributedLock
    {
        Task<IAsyncDisposable?> AcquireAsync(string key, TimeSpan expiry);
    }
}
