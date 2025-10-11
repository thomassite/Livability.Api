using System;
using System.Collections.Generic;

namespace Livability.Api.Dto;

/// <summary>
/// 警政署即時交通事故資料表 (NPA_TMA)
/// </summary>
public partial class NpaTma
{
    /// <summary>
    /// 自增主鍵，用於唯一識別每筆事故紀錄
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 發生年度
    /// </summary>
    public short? Year { get; set; }

    /// <summary>
    /// 發生月份 (1~12)
    /// </summary>
    public sbyte? Month { get; set; }

    /// <summary>
    /// 發生日期 (YYYY-MM-DD)
    /// </summary>
    public DateOnly? Date { get; set; }

    /// <summary>
    /// 發生時間 (HH:MM:SS)
    /// </summary>
    public TimeOnly? Time { get; set; }

    /// <summary>
    /// 事故類別 (A1: 死亡, A2: 受傷, A3: 財損)
    /// </summary>
    public string? AccidentType { get; set; }

    /// <summary>
    /// 處理單位名稱 (警察局/分局)
    /// </summary>
    public string? PoliceDepartment { get; set; }

    /// <summary>
    /// 事故發生地點文字描述
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// 事故經度座標
    /// </summary>
    public decimal? Longitude { get; set; }

    /// <summary>
    /// 事故緯度座標
    /// </summary>
    public decimal? Latitude { get; set; }
}
