using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using System.Security.Claims;

namespace Libraries.Web.Common.Middlewares
{
    public class ActiveUserMiddleware(RequestDelegate next)
    {
        private readonly RequestDelegate _next = next;
        private static readonly DistributedCacheEntryOptions CacheTtl = new()
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15)
        };

        public async Task Invoke(HttpContext context, IDistributedCache cache)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                try
                {
                    var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (userId is not null)
                        await cache.SetStringAsync($"active:{userId}", "1", CacheTtl);
                }
                catch
                {
                    // Redis недоступен — не фатал
                }
            }

            await _next(context);
        }
    }
}
