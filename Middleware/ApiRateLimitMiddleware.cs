using Livability.Api.Services;
using Livability.Api.Services.Interface;

namespace Livability.Api.Middleware
{
    public class ApiRateLimitMiddleware
    {
        private readonly RequestDelegate _next;
        public ApiRateLimitMiddleware(RequestDelegate next) => _next = next;

        public async Task InvokeAsync(HttpContext context, IApiQuotaService quota)
        {
            var path = context.Request.Path.Value?.ToLower() ?? string.Empty;

            // 排除 Hangfire Dashboard 與內部 API
            if (path.StartsWith("/hangfire"))
            {
                await _next(context);
                return;
            }

            // 可額外排除 Swagger、健康檢查、靜態檔案等
            if (path.StartsWith("/swagger") || path.StartsWith("/health") || path.StartsWith("/_framework"))
            {
                await _next(context);
                return;
            }
            var ip = context.Connection.RemoteIpAddress?.ToString();
            var ua = context.Request.Headers["User-Agent"].ToString();

            var (allowed, blocked) = await quota.TryConsumeAsync(
                provider: "livability_api",
                apiName: path,
                clientId: null,
                ip: ip,
                userAgent: ua,
                dailyLimit: 5000,
                hourlyLimit: 500,
                blockThreshold: 1000,  // 異常閾值
                blockMinutes: 30       // 封鎖 30 分鐘
            );

            if (blocked)
            {
                context.Response.StatusCode = 429;
                await context.Response.WriteAsync("Your IP has been temporarily blocked due to abnormal requests.");
                return;
            }

            if (!allowed)
            {
                context.Response.StatusCode = 429;
                await context.Response.WriteAsync("API rate limit exceeded.");
                return;
            }

            await _next(context);
        }
    }
}
