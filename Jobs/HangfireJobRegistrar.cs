using Hangfire;
using Livability.Api.Services;

namespace Livability.Api.Jobs
{
    /// <summary>
    /// 統一註冊所有 Hangfire 定期與一次性任務
    /// </summary>
    public static class HangfireJobRegistrar
    {
        /// <summary>
        /// 初始化所有背景 Job
        /// </summary>
        public static void RegisterJobs()
        {
            // 每天凌晨執行地理位置補齊任務
            RecurringJob.AddOrUpdate<MapGeocodeJob>(
                "geocode-agency-job",
                job => job.UpdateMissingAgenciesAsync(),
                Cron.Daily(3, 0)); // 每天 03:00 執行
            // IP定期解封
            RecurringJob.AddOrUpdate<ApiQuotaService>(
                "unblock-expired-ips",
                q => q.UnblockExpiredAsync(),
                Cron.Hourly());
        }
    }
}
