using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Livability.Api.Dto;

/// <summary>
/// 採購機關
/// </summary>
[Table("pcc_agency")]
public partial class PccAgency
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("agency_name")]
    [StringLength(100)]
    public string? AgencyName { get; set; }

    /// <summary>
    /// 經度座標
    /// </summary>
    [Column("longitude")]
    [Precision(11, 8)]
    public decimal? Longitude { get; set; }

    /// <summary>
    /// 緯度座標
    /// </summary>
    [Column("latitude")]
    [Precision(11, 8)]
    public decimal? Latitude { get; set; }
}
