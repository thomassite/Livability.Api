using System;
using System.Collections.Generic;

namespace Livability.Api.Dto;

/// <summary>
/// 採購網標案
/// </summary>
public partial class PccTenderMain
{
    /// <summary>
    /// 系統流水號
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Key
    /// </summary>
    public string TpamPk { get; set; } = null!;

    /// <summary>
    ///  種類
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    ///  機關編號
    /// </summary>
    public long? GeoLocationId { get; set; }

    /// <summary>
    ///  標案案號
    /// </summary>
    public string? TenderCaseNo { get; set; }

    /// <summary>
    /// 初始 標案案號
    /// </summary>
    public string? TenderCaseNoInit { get; set; }

    /// <summary>
    ///  標案名稱
    /// </summary>
    public string? TenderName { get; set; }

    /// <summary>
    /// 招標公告日期 (YYYY-MM-DD)
    /// </summary>
    public DateOnly? NoticeDate { get; set; }

    /// <summary>
    /// 截止投標日期
    /// </summary>
    public DateOnly? BidDeadline { get; set; }

    /// <summary>
    /// 預算金額（新台幣）
    /// </summary>
    public decimal? BudgetAmount { get; set; }

    /// <summary>
    ///  標案詳細網址
    /// </summary>
    public string? DetailUrl { get; set; }
}
