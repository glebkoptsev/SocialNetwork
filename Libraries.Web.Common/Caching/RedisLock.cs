using StackExchange.Redis;

namespace Libraries.Web.Common.Caching
{
    public class RedisLock(IConnectionMultiplexer redis) : IDistributedLock
    {
        public async Task<IAsyncDisposable?> AcquireAsync(string key, TimeSpan expiry)
        {
            var db = redis.GetDatabase();
            var lockValue = Guid.NewGuid().ToString();
            var acquired = await db.LockTakeAsync(key, lockValue, expiry);
            if (!acquired) return null;
            return new LockReleaser(db, key, lockValue);
        }

        private class LockReleaser(IDatabase db, string key, string lockValue) : IAsyncDisposable
        {
            public async ValueTask DisposeAsync()
            {
                await db.LockReleaseAsync(key, lockValue);
            }
        }
    }

    public interface IDistributedLock
    {
        Task<IAsyncDisposable?> AcquireAsync(string key, TimeSpan expiry);
    }
}
