using AutoMapper;
using CsvHelper;
using CsvHelper.Configuration;
using EFCore.BulkExtensions;
using Livability.Api.Context;
using Livability.Api.Dto;
using Livability.Api.Models.NpaTma;
using Livability.Api.Services.Interface;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Livability.Api.Services
{
    public class NpaTmaService : BaseService, INpaTmaService
    {
        public NpaTmaService(LivabilityContext db, IMapper mapper, ILogger<NpaTmaService> logger) : base(db, mapper, logger)
        {
        }
        /// <summary>
        /// 附近事故
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<List<NpaTmaLocationViewModel>> NpaTmaNearby(NpaTmaNearbyRequest request)
        {
            List<NpaTmaLocationViewModel> result = new();
            const double EarthRadius = 6371.0; // km

            // decimal → double
            double lat = (double)request.lat;
            double lon = (double)request.lon;
            double radiusKm = (double)request.radiusKm;

            // 可先用 bounding box 加速
            var latDelta = radiusKm / 111.0;
            var lonDelta = radiusKm / (111.0 * Math.Cos(lat * Math.PI / 180));

            var candidates = await _db.NpaTmas
                .Where(p =>
                    p.Latitude >= request.lat - (decimal)latDelta && p.Latitude <= request.lat + (decimal)latDelta &&
                    p.Longitude >= request.lon - (decimal)lonDelta && p.Longitude <= request.lon + (decimal)lonDelta && 
                    p.Month == request.month && 
                    p.Year == request.year)
                .ToListAsync();

            // 套 Haversine 計算距離
            var nearby = candidates
                .Select(p =>
                {
                    double plat = (double)p.Latitude;
                    double plon = (double)p.Longitude;

                    var dLat = (plat - lat) * Math.PI / 180;
                    var dLon = (plon - lon) * Math.PI / 180;
                    var a = Math.Pow(Math.Sin(dLat / 2), 2) +
                            Math.Cos(lat * Math.PI / 180) * Math.Cos(plat * Math.PI / 180) *
                            Math.Pow(Math.Sin(dLon / 2), 2);
                    var distance = 2 * EarthRadius * Math.Asin(Math.Sqrt(a));
                    return new { p, distance };
                })
                .Where(x => x.distance <= radiusKm)
                .OrderBy(x => x.distance)
                .ToList();

            _mapper.Map(nearby.Select(x => x.p), result);
            return result;
        }
        /// <summary>
        /// A1 & A2 交通事故 CSV 匯入
        /// </summary>
        /// <param name="csvStream"></param>
        /// <returns></returns>
        public async Task<int> ImportFromCsvAsync(Stream csvStream)
        {
            _db.ChangeTracker.AutoDetectChangesEnabled = false;

            // 設定 CsvHelper config（BadDataFound 只 log raw record）
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                BadDataFound = args =>
                {
                    _logger.LogWarning("Bad CSV raw row detected and skipped: {RawRecord}", args.RawRecord);
                },
                MissingFieldFound = null,
                HeaderValidated = null,
                IgnoreBlankLines = true,
                TrimOptions = TrimOptions.Trim,
                Delimiter = ","
            };

            using var reader = new StreamReader(csvStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            using var csv = new CsvReader(reader, config);

            // 讀 header
            if (!await csv.ReadAsync())
            {
                _logger.LogWarning("⚠️ CSV is empty.");
                return 0;
            }
            csv.ReadHeader();

            var records = new List<NpaTma>();
            int lineNo = 1;
            int skippedCount = 0;

            while (await csv.ReadAsync())
            {
                lineNo++;
                try
                {
                    // 使用 TryGetField 安全取欄位字串
                    csv.TryGetField("發生年度", out string? yearRaw);
                    csv.TryGetField("發生月份", out string? monthRaw);
                    csv.TryGetField("發生日期", out string? dateRaw);
                    csv.TryGetField("發生時間", out string? timeRaw);
                    csv.TryGetField("事故類別名稱", out string? accidentTypeRaw);
                    csv.TryGetField("處理單位名稱警局層", out string? policeDeptRaw);
                    csv.TryGetField("發生地點", out string? locationRaw);
                    csv.TryGetField("經度", out string? lonRaw);
                    csv.TryGetField("緯度", out string? latRaw);

                    // Trim 防護
                    yearRaw = yearRaw?.Trim();
                    monthRaw = monthRaw?.Trim();
                    dateRaw = dateRaw?.Trim();
                    timeRaw = timeRaw?.Trim();
                    accidentTypeRaw = accidentTypeRaw?.Trim();
                    policeDeptRaw = policeDeptRaw?.Trim();
                    locationRaw = locationRaw?.Trim();
                    lonRaw = lonRaw?.Trim();
                    latRaw = latRaw?.Trim();

                    // 跳過 footer / metadata 行（常見：發生年度為文字）
                    if (string.IsNullOrEmpty(yearRaw) || !Regex.IsMatch(yearRaw, @"^\d+$"))
                    {
                        skippedCount++;
                        _logger.LogDebug("跳過第 {LineNo} 行：發生年度非數字或空值 -> '{YearRaw}'", lineNo, yearRaw);
                        continue;
                    }

                    // 解析值（使用現有 helper + robust wrapper）
                    short? year = ParseHelpers.TryParseShort(yearRaw);
                    sbyte? month = ParseHelpers.TryParseSByte(monthRaw);
                    DateOnly? date = ParseHelpers.TryParseDateFlexibleRobust(dateRaw); // robust wrapper
                    TimeOnly? time = ParseHelpers.TryParseTimeFlexibleRobust(timeRaw); // robust wrapper
                    decimal? lon = ParseHelpers.TryParseDecimal(lonRaw);
                    decimal? lat = ParseHelpers.TryParseDecimal(latRaw);

                    // 必要欄位檢查（你可以依需求調整哪些欄位為必要）
                    if (year == null || month == null || date == null || time == null || lon == null || lat == null)
                    {
                        skippedCount++;
                        _logger.LogDebug("第 {LineNo} 行 欄位不足或解析失敗，略過。 year={Year}, month={Month}, date={Date}, time={Time}, lon={Lon}, lat={Lat}",
                            lineNo, year, month, date, time, lon, lat);
                        continue;
                    }

                    // 座標範圍檢查（台灣附近範圍保守設定）
                    if (lat < 16m || lat > 30m || lon < 116m || lon > 126m)
                    {
                        skippedCount++;
                        _logger.LogDebug("第 {LineNo} 行 座標疑似異常，略過。 lon={Lon}, lat={Lat}", lineNo, lon, lat);
                        continue;
                    }

                    var entity = new NpaTma
                    {
                        Year = year,
                        Month = month,
                        Date = date,
                        Time = time,
                        AccidentType = ParseHelpers.SafeTrim(accidentTypeRaw, 10),
                        PoliceDepartment = ParseHelpers.SafeTrim(policeDeptRaw, 100),
                        Location = ParseHelpers.SafeTrim(locationRaw, 500),
                        Longitude = lon,
                        Latitude = lat
                    };

                    records.Add(entity);
                }
                catch (Exception ex)
                {
                    skippedCount++;
                    _logger.LogWarning(ex, "⚠️ 第 {LineNo} 行解析失敗，略過。", lineNo);
                }
            }

            if (!records.Any())
            {
                _logger.LogWarning("⚠️ 無有效資料可匯入。跳過匯入。 (skipped {Skipped})", skippedCount);
                return 0;
            }

            // 去重
            var distinctRecords = records
                .GroupBy(r => new { r.Year, r.Month, r.Date, r.Time, r.Longitude, r.Latitude })
                .Select(g => g.First())
                .ToList();

            _logger.LogInformation("📦 匯入前共有 {Raw} 筆，去重後剩 {Distinct} 筆。已跳過 {Skipped} 筆非有效列。",
                records.Count, distinctRecords.Count, skippedCount);

            // Bulk insert/update 設定（沿用你原本設定）
            var bulkConfig = new BulkConfig
            {
                UseTempDB = false,
                UpdateByProperties = new List<string> { "Year", "Month", "Date", "Time", "Longitude", "Latitude" },
                BatchSize = 5000,
                CalculateStats = false,
                BulkCopyTimeout = 600
            };

            try
            {
                await _db.BulkInsertOrUpdateAsync(distinctRecords, bulkConfig);
                _logger.LogInformation("✅ 匯入完成，處理 {Count} 筆資料。", distinctRecords.Count);
                return distinctRecords.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 匯入時發生錯誤。");
                throw;
            }
        }
    }
}
