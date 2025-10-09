using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Livability.Api.Dto;

/// <summary>
/// 採購網標案
/// </summary>
[Table("pcc_tender_main")]
public partial class PccTenderMain
{
    /// <summary>
    /// 系統流水號
    /// </summary>
    [Key]
    [Column("id")]
    public long Id { get; set; }

    /// <summary>
    /// Key
    /// </summary>
    [Column("tpam_pk")]
    [StringLength(100)]
    public string TpamPk { get; set; } = null!;

    /// <summary>
    ///  種類
    /// </summary>
    [Column("category")]
    [StringLength(50)]
    public string? Category { get; set; }

    /// <summary>
    ///  機關編號
    /// </summary>
    [Column("ppc_agency_id")]
    public long? PpcAgencyId { get; set; }

    /// <summary>
    ///  標案案號
    /// </summary>
    [Column("tender_case_no")]
    [StringLength(100)]
    public string? TenderCaseNo { get; set; }

    /// <summary>
    ///  標案名稱
    /// </summary>
    [Column("tender_name")]
    [StringLength(500)]
    public string? TenderName { get; set; }

    /// <summary>
    /// 招標公告日期 (YYYY-MM-DD)
    /// </summary>
    [Column("notice_date")]
    public DateOnly? NoticeDate { get; set; }

    /// <summary>
    /// 截止投標日期
    /// </summary>
    [Column("bid_deadline")]
    public DateOnly? BidDeadline { get; set; }

    /// <summary>
    /// 預算金額（新台幣）
    /// </summary>
    [Column("budget_amount")]
    [Precision(18, 2)]
    public decimal? BudgetAmount { get; set; }

    /// <summary>
    ///  標案詳細網址
    /// </summary>
    [Column("detail_url")]
    [StringLength(500)]
    public string? DetailUrl { get; set; }
}
