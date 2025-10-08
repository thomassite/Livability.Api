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

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseMySql("server=18.177.57.10;port=3306;database=livability;user=npa_user;password=S!lver2024@DB;allowpublickeyretrieval=True;sslmode=None;AllowLoadLocalInfile=true;", Microsoft.EntityFrameworkCore.ServerVersion.Parse("8.0.43-mysql"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

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

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
