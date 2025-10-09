using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Livability.Api.Dto;

/// <summary>
/// 警政署即時交通事故資料表 (NPA_TMA)
/// </summary>
[Table("npa_tma")]
[Index("Date", Name = "idx_date")]
[Index("Latitude", "Longitude", Name = "idx_latlng")]
[Index("AccidentType", Name = "idx_type")]
[Index("Year", "Month", "Date", "Time", "Longitude", "Latitude", Name = "idx_unique", IsUnique = true)]
public partial class NpaTma
{
    /// <summary>
    /// 自增主鍵，用於唯一識別每筆事故紀錄
    /// </summary>
    [Key]
    [Column("id")]
    public long Id { get; set; }

    /// <summary>
    /// 發生年度
    /// </summary>
    [Column("year")]
    public short? Year { get; set; }

    /// <summary>
    /// 發生月份 (1~12)
    /// </summary>
    [Column("month")]
    public sbyte? Month { get; set; }

    /// <summary>
    /// 發生日期 (YYYY-MM-DD)
    /// </summary>
    [Column("date")]
    public DateOnly? Date { get; set; }

    /// <summary>
    /// 發生時間 (HH:MM:SS)
    /// </summary>
    [Column("time", TypeName = "time")]
    public TimeOnly? Time { get; set; }

    /// <summary>
    /// 事故類別 (A1: 死亡, A2: 受傷, A3: 財損)
    /// </summary>
    [Column("accident_type")]
    [StringLength(10)]
    public string? AccidentType { get; set; }

    /// <summary>
    /// 處理單位名稱 (警察局/分局)
    /// </summary>
    [Column("police_department")]
    [StringLength(100)]
    public string? PoliceDepartment { get; set; }

    /// <summary>
    /// 事故發生地點文字描述
    /// </summary>
    [Column("location")]
    [StringLength(500)]
    public string? Location { get; set; }

    /// <summary>
    /// 事故經度座標
    /// </summary>
    [Column("longitude")]
    [Precision(11, 8)]
    public decimal? Longitude { get; set; }

    /// <summary>
    /// 事故緯度座標
    /// </summary>
    [Column("latitude")]
    [Precision(11, 8)]
    public decimal? Latitude { get; set; }
}
