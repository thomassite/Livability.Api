using AutoMapper;
using Livability.Api.Context;
using Livability.Api.Dto;
using Livability.Api.Services.Interface;
using Microsoft.EntityFrameworkCore;

namespace Livability.Api.Services
{
    public class ApiQuotaService: BaseService, IApiQuotaService
    {

        public ApiQuotaService(LivabilityContext db, IMapper mapper, ILogger<ApiQuotaService> logger)
        : base(db, mapper, logger)
        {

        }

        public async Task<(bool allowed, bool blocked)> TryConsumeAsync(
            string provider, string apiName, string? clientId,
            string? ip, string? userAgent,
            int dailyLimit, int hourlyLimit, int blockThreshold, int blockMinutes)
        {
            var now = DateTime.UtcNow;
            var today = DateOnly.FromDateTime(now);
            var hour = now.Hour;

            // ✅ 查詢時使用 DateOnly 比較
            var quota = await _db.ApiUsageQuota
                .FirstOrDefaultAsync(q =>
                    q.Provider == provider &&
                    q.ApiName == apiName &&
                    q.ClientId == clientId &&
                    q.IpAddress == ip &&
                    q.Date == today);

            if (quota == null)
            {
                quota = new ApiUsageQuotum
                {
                    Provider = provider,
                    ApiName = apiName,
                    ClientId = clientId,
                    IpAddress = ip,
                    UserAgent = userAgent,
                    Date = today,
                    Hour = hour,
                    UsageCount = 0, // ✅ 改成明確名稱避免和 LINQ.Count() 衝突
                    LimitPerDay = dailyLimit,
                    LimitPerHour = hourlyLimit
                };
                _db.ApiUsageQuota.Add(quota);
            }

            // 🚫 已被封鎖
            if (quota.BlockedUntil.HasValue && quota.BlockedUntil > now)
            {
                _logger.LogWarning("🚫 IP {Ip} 已封鎖至 {Until}", ip, quota.BlockedUntil);
                return (false, true);
            }

            // 🚫 超過每日上限
            if (quota.UsageCount >= quota.LimitPerDay)
            {
                _logger.LogWarning("⛔ {Provider}/{ApiName} 超出每日上限 ({Limit}) for {Ip}", provider, apiName, quota.LimitPerDay, ip);
                return (false, false);
            }

            // ✅ 增加計數
            quota.UsageCount++;
            quota.UpdatedAt = now;

            // 🧨 短時間爆量自動封鎖（防爬蟲）
            if (quota.UsageCount >= blockThreshold)
            {
                quota.BlockedUntil = now.AddMinutes(blockMinutes);
                _logger.LogWarning("🧨 檢測到爆量流量，自動封鎖 IP {Ip} {Minutes} 分鐘", ip, blockMinutes);
            }

            await _db.SaveChangesAsync();

            _logger.LogInformation("📊 {Provider}/{ApiName} {Count}/{Limit} - IP:{Ip}", provider, apiName, quota.UsageCount, quota.LimitPerDay, ip);
            return (true, false);
        }

        public async Task UnblockExpiredAsync()
        {
            var rows = await _db.Database.ExecuteSqlRawAsync("UPDATE api_usage_quota SET blocked_until = NULL WHERE blocked_until <= UTC_TIMESTAMP()");
            if (rows > 0)
                _logger.LogInformation("♻️ 已解除 {Count} 個過期封鎖", rows);
        }
    }
}
