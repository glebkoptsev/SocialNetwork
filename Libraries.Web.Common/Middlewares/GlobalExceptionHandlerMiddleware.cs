using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Libraries.Web.Common.Middlewares
{
    public class GlobalExceptionHandlerMiddleware(RequestDelegate next)
    {
        private readonly RequestDelegate _next = next;

        public async Task Invoke(HttpContext context, ILogger<GlobalExceptionHandlerMiddleware> logger)
        {
            try
            {
                await _next(context);
            }
            catch (UnauthorizedAccessException ex)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = ex.Message }));
            }
            catch (KeyNotFoundException ex)
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = ex.Message }));
            }
            catch (OperationCanceledException)
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                context.Response.Headers.RetryAfter = "5";
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(new
                    { error = "Service temporarily unavailable" }));
            }
            catch (Exception ex)
            {
                var isInfra = ex.GetType().Name.Contains("Npgsql")
                    || ex.GetType().Name.Contains("Redis")
                    || ex.GetType().Name.Contains("SocketException");
                if (isInfra)
                {
                    logger.LogWarning(ex, "Infrastructure unavailable on {Path}", context.Request.Path);
                    context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                    context.Response.Headers.RetryAfter = "5";
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new
                        { error = "Service temporarily unavailable" }));
                }
                else
                {
                    logger.LogError(ex, "Unhandled exception on {Path}", context.Request.Path);
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(JsonSerializer.Serialize(new
                        { error = "Internal server error" }));
                }
            }
        }
    }
}
