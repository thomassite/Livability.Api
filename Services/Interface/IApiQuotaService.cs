namespace Livability.Api.Services.Interface
{
    public interface IApiQuotaService
    {
        Task<(bool allowed, bool blocked)> TryConsumeAsync(
            string provider, string apiName, string? clientId,
            string? ip, string? userAgent,
            int dailyLimit, int hourlyLimit, int blockThreshold, int blockMinutes);
        Task UnblockExpiredAsync();
    }
}
