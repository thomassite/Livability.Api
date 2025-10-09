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

    public virtual DbSet<NpaTma> NpaTmas { get; set; }

    public virtual DbSet<PccAgency> PccAgencies { get; set; }

    public virtual DbSet<PccTenderMain> PccTenderMains { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseMySql("server=18.177.57.10;port=3306;database=livability;uid=npa_user;pwd=S!lver2024@DB", Microsoft.EntityFrameworkCore.ServerVersion.Parse("8.0.43-mysql"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<NpaTma>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("npa_tma", tb => tb.HasComment("警政署即時交通事故資料表 (NPA_TMA)"));

            entity.HasIndex(e => e.Location, "idx_location").HasAnnotation("MySql:IndexPrefixLength", new[] { 100 });

            entity.Property(e => e.Id).HasComment("自增主鍵，用於唯一識別每筆事故紀錄");
            entity.Property(e => e.AccidentType).HasComment("事故類別 (A1: 死亡, A2: 受傷, A3: 財損)");
            entity.Property(e => e.Date).HasComment("發生日期 (YYYY-MM-DD)");
            entity.Property(e => e.Latitude).HasComment("事故緯度座標");
            entity.Property(e => e.Location).HasComment("事故發生地點文字描述");
            entity.Property(e => e.Longitude).HasComment("事故經度座標");
            entity.Property(e => e.Month).HasComment("發生月份 (1~12)");
            entity.Property(e => e.PoliceDepartment).HasComment("處理單位名稱 (警察局/分局)");
            entity.Property(e => e.Time).HasComment("發生時間 (HH:MM:SS)");
            entity.Property(e => e.Year).HasComment("發生年度");
        });

        modelBuilder.Entity<PccAgency>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("pcc_agency", tb => tb.HasComment("採購機關"));

            entity.Property(e => e.Latitude).HasComment("緯度座標");
            entity.Property(e => e.Longitude).HasComment("經度座標");
        });

        modelBuilder.Entity<PccTenderMain>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("pcc_tender_main", tb => tb.HasComment("採購網標案"));

            entity.Property(e => e.Id).HasComment("系統流水號");
            entity.Property(e => e.BidDeadline).HasComment("截止投標日期");
            entity.Property(e => e.BudgetAmount).HasComment("預算金額（新台幣）");
            entity.Property(e => e.Category).HasComment(" 種類");
            entity.Property(e => e.DetailUrl).HasComment(" 標案詳細網址");
            entity.Property(e => e.NoticeDate).HasComment("招標公告日期 (YYYY-MM-DD)");
            entity.Property(e => e.PpcAgencyId).HasComment(" 機關編號");
            entity.Property(e => e.TenderCaseNo).HasComment(" 標案案號");
            entity.Property(e => e.TenderName).HasComment(" 標案名稱");
            entity.Property(e => e.TpamPk).HasComment("Key");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
