using System;
using System.Collections.Generic;

namespace Livability.Api.Dto;

/// <summary>
/// API 使用配額與防爬蟲紀錄表，用於追蹤每日/每小時 API 使用次數與異常封鎖狀態
/// </summary>
public partial class ApiUsageQuotum
{
    /// <summary>
    /// 系統流水號
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// API 提供者 (例如: google、moenv、openai、livability-api)
    /// </summary>
    public string Provider { get; set; } = null!;

    /// <summary>
    /// API 類型或名稱 (例如: geocode、air_quality、noise、tender_crawler)
    /// </summary>
    public string ApiName { get; set; } = null!;

    /// <summary>
    /// 呼叫端識別 (例如: livability-service、frontend、external-partner)
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// 請求來源 IP 位址 (用於防爬蟲或黑名單控管)
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// 使用者代理字串 (User-Agent，用於識別客戶端類型)
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// 統計日期 (UTC 時區)
    /// </summary>
    public DateOnly Date { get; set; }

    /// <summary>
    /// 統計小時 (0-23)，用於每小時限流監控
    /// </summary>
    public int? Hour { get; set; }

    /// <summary>
    /// 當天已使用次數 (呼叫計數)
    /// </summary>
    public int UsageCount { get; set; }

    /// <summary>
    /// 每日配額上限
    /// </summary>
    public int LimitPerDay { get; set; }

    /// <summary>
    /// 每小時配額上限 (可為 NULL 表示不啟用)
    /// </summary>
    public int? LimitPerHour { get; set; }

    /// <summary>
    /// 封鎖到期時間 (若非空則代表該 IP / client 暫時被封鎖)
    /// </summary>
    public DateTime? BlockedUntil { get; set; }

    /// <summary>
    /// 最後更新時間 (自動更新)
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
