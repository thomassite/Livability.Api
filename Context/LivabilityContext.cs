using System;
using System.Collections.Generic;
using Livability.Api.Dto;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace Livability.Api.Context;

public partial class LivabilityContext : DbContext
{
    public LivabilityContext()
    {
    }

    public LivabilityContext(DbContextOptions<LivabilityContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ApiUsageQuotum> ApiUsageQuota { get; set; }

    public virtual DbSet<GeoLocation> GeoLocations { get; set; }

    public virtual DbSet<NpaTma> NpaTmas { get; set; }

    public virtual DbSet<PccTenderMain> PccTenderMains { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseMySql("server=18.177.57.10;port=3306;database=livability;user=npa_user;password=S!lver2024@DB;allowpublickeyretrieval=True;sslmode=None", Microsoft.EntityFrameworkCore.ServerVersion.Parse("8.0.43-mysql"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<ApiUsageQuotum>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("api_usage_quota", tb => tb.HasComment("API 使用配額與防爬蟲紀錄表，用於追蹤每日/每小時 API 使用次數與異常封鎖狀態"));

            entity.HasIndex(e => new { e.Provider, e.ApiName, e.ClientId, e.IpAddress, e.Date }, "uq_provider_api_client_ip_date").IsUnique();

            entity.Property(e => e.Id)
                .HasComment("系統流水號")
                .HasColumnName("id");
            entity.Property(e => e.ApiName)
                .HasMaxLength(100)
                .HasComment("API 類型或名稱 (例如: geocode、air_quality、noise、tender_crawler)")
                .HasColumnName("api_name");
            entity.Property(e => e.BlockedUntil)
                .HasComment("封鎖到期時間 (若非空則代表該 IP / client 暫時被封鎖)")
                .HasColumnType("datetime")
                .HasColumnName("blocked_until");
            entity.Property(e => e.ClientId)
                .HasMaxLength(100)
                .HasComment("呼叫端識別 (例如: livability-service、frontend、external-partner)")
                .HasColumnName("client_id");
            entity.Property(e => e.Date)
                .HasComment("統計日期 (UTC 時區)")
                .HasColumnName("date");
            entity.Property(e => e.Hour)
                .HasComment("統計小時 (0-23)，用於每小時限流監控")
                .HasColumnName("hour");
            entity.Property(e => e.IpAddress)
                .HasMaxLength(45)
                .HasComment("請求來源 IP 位址 (用於防爬蟲或黑名單控管)")
                .HasColumnName("ip_address");
            entity.Property(e => e.LimitPerDay)
                .HasDefaultValueSql("'2500'")
                .HasComment("每日配額上限")
                .HasColumnName("limit_per_day");
            entity.Property(e => e.LimitPerHour)
                .HasComment("每小時配額上限 (可為 NULL 表示不啟用)")
                .HasColumnName("limit_per_hour");
            entity.Property(e => e.Provider)
                .HasMaxLength(100)
                .HasComment("API 提供者 (例如: google、moenv、openai、livability-api)")
                .HasColumnName("provider");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("最後更新時間 (自動更新)")
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.UsageCount)
                .HasComment("當天已使用次數 (呼叫計數)")
                .HasColumnName("usage_count");
            entity.Property(e => e.UserAgent)
                .HasMaxLength(255)
                .HasComment("使用者代理字串 (User-Agent，用於識別客戶端類型)")
                .HasColumnName("user_agent");
        });

        modelBuilder.Entity<GeoLocation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("geo_location", tb => tb.HasComment("Google Maps Geocoding API 地理座標紀錄表"));

            entity.HasIndex(e => new { e.Latitude, e.Longitude }, "idx_lat_lng");

            entity.HasIndex(e => e.PlaceId, "idx_place_id");

            entity.HasIndex(e => e.PlaceName, "idx_place_name");

            entity.Property(e => e.Id)
                .HasComment("流水號")
                .HasColumnName("id");
            entity.Property(e => e.AdministrativeAreaLevel1)
                .HasMaxLength(100)
                .HasComment("縣市 (例如: 臺中市)")
                .HasColumnName("administrative_area_level_1");
            entity.Property(e => e.AdministrativeAreaLevel2)
                .HasMaxLength(100)
                .HasComment("區 (例如: 北區)")
                .HasColumnName("administrative_area_level_2");
            entity.Property(e => e.AdministrativeAreaLevel3)
                .HasMaxLength(100)
                .HasComment("里/鄰 (例如: 中山里)")
                .HasColumnName("administrative_area_level_3");
            entity.Property(e => e.Country)
                .HasMaxLength(100)
                .HasComment("國家名稱")
                .HasColumnName("country");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasComment("建立時間")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.FormattedAddress)
                .HasMaxLength(500)
                .HasComment("Google 格式化後的完整地址")
                .HasColumnName("formatted_address");
            entity.Property(e => e.JsonRaw)
                .HasComment("原始 Google Maps 回傳 JSON")
                .HasColumnType("json")
                .HasColumnName("json_raw");
            entity.Property(e => e.Latitude)
                .HasPrecision(10, 7)
                .HasComment("緯度")
                .HasColumnName("latitude");
            entity.Property(e => e.LocationType)
                .HasMaxLength(50)
                .HasComment("精度類型 (ROOFTOP / APPROXIMATE / RANGE_INTERPOLATED 等)")
                .HasColumnName("location_type");
            entity.Property(e => e.Longitude)
                .HasPrecision(10, 7)
                .HasComment("經度")
                .HasColumnName("longitude");
            entity.Property(e => e.PartialMatch)
                .HasDefaultValueSql("'0'")
                .HasComment("是否為模糊匹配 (1=True)")
                .HasColumnName("partial_match");
            entity.Property(e => e.PlaceId)
                .HasMaxLength(100)
                .HasComment("Google 唯一識別 ID")
                .HasColumnName("place_id");
            entity.Property(e => e.PlaceName)
                .HasComment("原始查詢名稱")
                .HasColumnName("place_name");
            entity.Property(e => e.PostalCode)
                .HasMaxLength(20)
                .HasComment("郵遞區號")
                .HasColumnName("postal_code");
            entity.Property(e => e.Route)
                .HasMaxLength(150)
                .HasComment("路名 (例如: 中山路)")
                .HasColumnName("route");
            entity.Property(e => e.StreetNumber)
                .HasMaxLength(50)
                .HasComment("門牌號 (例如: 96號)")
                .HasColumnName("street_number");
            entity.Property(e => e.Types)
                .HasMaxLength(255)
                .HasComment("地點類型 (例如: establishment, point_of_interest)")
                .HasColumnName("types");
        });

        modelBuilder.Entity<NpaTma>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("npa_tma", tb => tb.HasComment("警政署即時交通事故資料表 (NPA_TMA)"));

            entity.HasIndex(e => e.Date, "idx_date");

            entity.HasIndex(e => new { e.Latitude, e.Longitude }, "idx_latlng");

            entity.HasIndex(e => e.Location, "idx_location").HasAnnotation("MySql:IndexPrefixLength", new[] { 100 });

            entity.HasIndex(e => e.AccidentType, "idx_type");

            entity.HasIndex(e => new { e.Year, e.Month, e.Date, e.Time, e.Longitude, e.Latitude }, "idx_unique").IsUnique();

            entity.Property(e => e.Id)
                .HasComment("自增主鍵，用於唯一識別每筆事故紀錄")
                .HasColumnName("id");
            entity.Property(e => e.AccidentType)
                .HasMaxLength(10)
                .HasComment("事故類別 (A1: 死亡, A2: 受傷, A3: 財損)")
                .HasColumnName("accident_type");
            entity.Property(e => e.Date)
                .HasComment("發生日期 (YYYY-MM-DD)")
                .HasColumnName("date");
            entity.Property(e => e.Latitude)
                .HasPrecision(11, 8)
                .HasComment("事故緯度座標")
                .HasColumnName("latitude");
            entity.Property(e => e.Location)
                .HasMaxLength(500)
                .HasComment("事故發生地點文字描述")
                .HasColumnName("location");
            entity.Property(e => e.Longitude)
                .HasPrecision(11, 8)
                .HasComment("事故經度座標")
                .HasColumnName("longitude");
            entity.Property(e => e.Month)
                .HasComment("發生月份 (1~12)")
                .HasColumnName("month");
            entity.Property(e => e.PoliceDepartment)
                .HasMaxLength(100)
                .HasComment("處理單位名稱 (警察局/分局)")
                .HasColumnName("police_department");
            entity.Property(e => e.Time)
                .HasComment("發生時間 (HH:MM:SS)")
                .HasColumnType("time")
                .HasColumnName("time");
            entity.Property(e => e.Year)
                .HasComment("發生年度")
                .HasColumnName("year");
        });

        modelBuilder.Entity<PccTenderMain>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("pcc_tender_main", tb => tb.HasComment("採購網標案"));

            entity.Property(e => e.Id)
                .HasComment("系統流水號")
                .HasColumnName("id");
            entity.Property(e => e.BidDeadline)
                .HasComment("截止投標日期")
                .HasColumnName("bid_deadline");
            entity.Property(e => e.BudgetAmount)
                .HasPrecision(18, 2)
                .HasComment("預算金額（新台幣）")
                .HasColumnName("budget_amount");
            entity.Property(e => e.Category)
                .HasMaxLength(50)
                .HasComment(" 種類")
                .HasColumnName("category");
            entity.Property(e => e.DetailUrl)
                .HasMaxLength(500)
                .HasComment(" 標案詳細網址")
                .HasColumnName("detail_url");
            entity.Property(e => e.GeoLocationId)
                .HasComment(" 機關編號")
                .HasColumnName("geo_location_id");
            entity.Property(e => e.NoticeDate)
                .HasComment("招標公告日期 (YYYY-MM-DD)")
                .HasColumnName("notice_date");
            entity.Property(e => e.TenderCaseNo)
                .HasMaxLength(100)
                .HasComment(" 標案案號")
                .HasColumnName("tender_case_no");
            entity.Property(e => e.TenderCaseNoInit)
                .HasMaxLength(100)
                .HasComment("初始 標案案號")
                .HasColumnName("tender_case_no_init");
            entity.Property(e => e.TenderName)
                .HasMaxLength(500)
                .HasComment(" 標案名稱")
                .HasColumnName("tender_name");
            entity.Property(e => e.TpamPk)
                .HasMaxLength(100)
                .HasComment("Key")
                .HasColumnName("tpam_pk");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
