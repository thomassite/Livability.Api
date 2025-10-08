using System.Globalization;
using CsvHelper;
using EFCore.BulkExtensions;
using Livability.Api.Context;
using Livability.Api.Dto;

namespace Livability.Api.Services
{
    public class NpaTmaImportService
    {
        private readonly LivabilityContext _db;
        private readonly ILogger<NpaTmaImportService> _logger;

        public NpaTmaImportService(LivabilityContext db, ILogger<NpaTmaImportService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<int> ImportFromCsvAsync(Stream csvStream)
        {
            _db.ChangeTracker.AutoDetectChangesEnabled = false;

            using var reader = new StreamReader(csvStream);
            using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);

            await csv.ReadAsync();
            csv.ReadHeader();

            var records = new List<NpaTma>();
            int lineNo = 1;

            while (await csv.ReadAsync())
            {
                lineNo++;
                try
                {
                    var entity = new NpaTma
                    {
                        Year = TryParseShort(csv.GetField("發生年度")),
                        Month = TryParseSByte(csv.GetField("發生月份")),
                        Date = TryParseDateFlexible(csv.GetField("發生日期")),
                        Time = TryParseTimeFlexible(csv.GetField("發生時間")),
                        AccidentType = SafeTrim(csv.GetField("事故類別名稱"), 10),
                        PoliceDepartment = SafeTrim(csv.GetField("處理單位名稱警局層"), 100),
                        Location = SafeTrim(csv.GetField("發生地點"), 500),
                        Longitude = TryParseDecimal(csv.GetField("經度")),
                        Latitude = TryParseDecimal(csv.GetField("緯度"))
                    };

                    if (entity.Year == null || entity.Month == null || entity.Date == null ||
                        entity.Time == null || entity.Longitude == null || entity.Latitude == null)
                        continue;

                    records.Add(entity);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "⚠️ 第 {LineNo} 行解析失敗，略過。", lineNo);
                }
            }

            if (!records.Any())
            {
                _logger.LogWarning("⚠️ 無有效資料可匯入。");
                return 0;
            }

            // 🧩 先去重複
            var distinctRecords = records
                .GroupBy(r => new { r.Year, r.Month, r.Date, r.Time, r.Longitude, r.Latitude })
                .Select(g => g.First())
                .ToList();

            _logger.LogInformation("📦 匯入前共有 {Raw} 筆，去重後剩 {Distinct} 筆。",
                records.Count, distinctRecords.Count);

            // 🚀 匯入設定
            var bulkConfig = new BulkConfig
            {
                UseTempDB = false, // ✅ 不再使用 temp table
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

        #region 🔧 Helper
        private static DateOnly? TryParseDateFlexible(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            input = input.Trim();
            if (input.Length == 8 && input.All(char.IsDigit))
            {
                try
                {
                    var year = int.Parse(input[..4]);
                    var month = int.Parse(input.Substring(4, 2));
                    var day = int.Parse(input.Substring(6, 2));
                    return new DateOnly(year, month, day);
                }
                catch { return null; }
            }
            if (DateOnly.TryParse(input, out var d))
                return d;
            if (DateTime.TryParse(input, out var dt))
                return DateOnly.FromDateTime(dt);
            return null;
        }

        private static TimeOnly? TryParseTimeFlexible(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            input = input.Trim();
            if (int.TryParse(input, out var num))
            {
                var s = num.ToString().PadLeft(4, '0');
                try { return new TimeOnly(int.Parse(s[..2]), int.Parse(s.Substring(2, 2))); }
                catch { return null; }
            }
            if (TimeOnly.TryParse(input, out var t)) return t;
            if (DateTime.TryParse(input, out var dt)) return TimeOnly.FromDateTime(dt);
            return null;
        }

        private static short? TryParseShort(string? input) => short.TryParse(input?.Trim(), out var s) ? s : null;
        private static sbyte? TryParseSByte(string? input) => sbyte.TryParse(input?.Trim(), out var s) ? s : null;
        private static decimal? TryParseDecimal(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            if (decimal.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                return Math.Round(d, 8);
            return null;
        }

        private static string? SafeTrim(string? input, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            input = input.Trim();
            return input.Length > maxLength ? input[..maxLength] : input;
        }
        #endregion
    }
}
