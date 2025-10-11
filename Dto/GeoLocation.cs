using System;
using System.Collections.Generic;

namespace Livability.Api.Dto;

/// <summary>
/// Google Maps Geocoding API 地理座標紀錄表
/// </summary>
public partial class GeoLocation
{
    /// <summary>
    /// 流水號
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 原始查詢名稱
    /// </summary>
    public string PlaceName { get; set; } = null!;

    /// <summary>
    /// Google 格式化後的完整地址
    /// </summary>
    public string? FormattedAddress { get; set; }

    /// <summary>
    /// 郵遞區號
    /// </summary>
    public string? PostalCode { get; set; }

    /// <summary>
    /// 國家名稱
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    /// 縣市 (例如: 臺中市)
    /// </summary>
    public string? AdministrativeAreaLevel1 { get; set; }

    /// <summary>
    /// 區 (例如: 北區)
    /// </summary>
    public string? AdministrativeAreaLevel2 { get; set; }

    /// <summary>
    /// 里/鄰 (例如: 中山里)
    /// </summary>
    public string? AdministrativeAreaLevel3 { get; set; }

    /// <summary>
    /// 路名 (例如: 中山路)
    /// </summary>
    public string? Route { get; set; }

    /// <summary>
    /// 門牌號 (例如: 96號)
    /// </summary>
    public string? StreetNumber { get; set; }

    /// <summary>
    /// 緯度
    /// </summary>
    public decimal? Latitude { get; set; }

    /// <summary>
    /// 經度
    /// </summary>
    public decimal? Longitude { get; set; }

    /// <summary>
    /// 精度類型 (ROOFTOP / APPROXIMATE / RANGE_INTERPOLATED 等)
    /// </summary>
    public string? LocationType { get; set; }

    /// <summary>
    /// Google 唯一識別 ID
    /// </summary>
    public string? PlaceId { get; set; }

    /// <summary>
    /// 是否為模糊匹配 (1=True)
    /// </summary>
    public bool? PartialMatch { get; set; }

    /// <summary>
    /// 地點類型 (例如: establishment, point_of_interest)
    /// </summary>
    public string? Types { get; set; }

    /// <summary>
    /// 原始 Google Maps 回傳 JSON
    /// </summary>
    public string? JsonRaw { get; set; }

    /// <summary>
    /// 建立時間
    /// </summary>
    public DateTime? CreatedAt { get; set; }
}
