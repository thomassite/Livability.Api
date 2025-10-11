using HtmlAgilityPack;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

public static class ParseHelpers
{
    // ---------------------------
    // 假設你已經有的兩個方法（不要改）
    // ---------------------------
    // public static DateOnly? TryParseDateFlexible(string? input) { ... }  // keep as-is
    // public static TimeOnly? TryParseTimeFlexible(string? input) { ... }  // keep as-is

    // ---------------------------
    // 新：Robust wrapper for Date
    public static DateOnly? TryParseDateFlexible(string? input)
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

    public static TimeOnly? TryParseTimeFlexible(string? input)
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

    // --------- Robust wrappers (先試原本方法，失敗則嘗試更多情況)
    public static DateOnly? TryParseDateFlexibleRobust(string? input)
    {
        // 1) 優先使用原本簡潔版本
        var baseResult = TryParseDateFlexible(input);
        if (baseResult != null) return baseResult;

        if (string.IsNullOrWhiteSpace(input)) return null;
        var s = Normalize(input);

        // 處理 "民國" 或純 ROC 年的情況 (e.g., 114/10/01 或 民國114年10月1日)
        var rocPattern = new Regex(@"(?i)(?:民國|ROC|R\.O\.C\.)?\s*([0-9]{2,3})(?:[^\d]+([0-9]{1,2}))?(?:[^\d]+([0-9]{1,2}))?");
        var m = rocPattern.Match(s);
        if (m.Success && int.TryParse(m.Groups[1].Value, out var maybeRoc))
        {
            var year = RocToAd(maybeRoc);
            int month = 1, day = 1;
            if (m.Groups.Count >= 3 && !string.IsNullOrEmpty(m.Groups[2].Value))
                int.TryParse(m.Groups[2].Value, out month);
            if (m.Groups.Count >= 4 && !string.IsNullOrEmpty(m.Groups[3].Value))
                int.TryParse(m.Groups[3].Value, out day);
            if (IsValidYmd(year, month, day)) return new DateOnly(year, month, day);
        }

        // 去除中文年/月/日符號並嘗試常見格式
        if (s.Contains("年") || s.Contains("月") || s.Contains("日"))
        {
            var tmp = s.Replace("年", "/").Replace("月", "/").Replace("日", "").Replace("．", ".").Replace("。", "");
            tmp = Regex.Replace(tmp, @"\s+", "");
            if (TryParseDateWithCommonFormats(tmp, out var res)) return res;

            // 若第一段為 2~3 位數，當作 ROC 處理
            var parts = tmp.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 1 && Regex.IsMatch(parts[0], @"^\d{2,3}$") && int.TryParse(parts[0], out var r))
            {
                var y = RocToAd(r);
                int mo = 1, da = 1;
                if (parts.Length >= 2) int.TryParse(parts[1], out mo);
                if (parts.Length >= 3) int.TryParse(parts[2], out da);
                if (IsValidYmd(y, mo, da)) return new DateOnly(y, mo, da);
            }
        }

        // 處理 yyyyMMdd 純數字
        if (Regex.IsMatch(s, @"^\d{8}$"))
        {
            if (DateOnly.TryParseExact(s, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d8))
                return d8;
        }

        // 嘗試多種格式
        var formats = new[]
        {
                "yyyy/MM/dd","yyyy/M/d","yyyy-MM-dd","yyyy-M-d","yyyy.MM.dd","yyyy.M.d",
                "yyyy/MM","yyyyMM","yyyy"
            };
        if (DateOnly.TryParseExact(s, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dtExact))
            return dtExact;

        // 最後 fallback to DateTime parsing
        if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dtt))
            return DateOnly.FromDateTime(dtt);
        if (DateTime.TryParse(s, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out var dtt2))
            return DateOnly.FromDateTime(dtt2);

        // 嘗試從數字序列抽取 (保守)
        var nums = Regex.Matches(s, @"\d+").Cast<Match>().Select(x => x.Value).ToArray();
        if (nums.Length >= 3
            && int.TryParse(nums[0], out var a0)
            && int.TryParse(nums[1], out var a1)
            && int.TryParse(nums[2], out var a2))
        {
            if (a0 < 1000)
            {
                var year = RocToAd(a0);
                if (IsValidYmd(year, a1, a2)) return new DateOnly(year, a1, a2);
            }
            else
            {
                if (IsValidYmd(a0, a1, a2)) return new DateOnly(a0, a1, a2);
            }
        }

        return null;
    }

    public static TimeOnly? TryParseTimeFlexibleRobust(string? input)
    {
        var baseResult = TryParseTimeFlexible(input);
        if (baseResult != null) return baseResult;

        if (string.IsNullOrWhiteSpace(input)) return null;
        var s = Normalize(input);

        // 取出純數字（處理 HHmmss 或 HHmm）
        var digitsOnly = Regex.Replace(s, @"\D", "");
        if (digitsOnly.Length == 6)
        {
            if (int.TryParse(digitsOnly[..2], out var hh)
                && int.TryParse(digitsOnly.Substring(2, 2), out var mm)
                && int.TryParse(digitsOnly.Substring(4, 2), out var ss))
            {
                if (hh >= 0 && hh < 24 && mm >= 0 && mm < 60 && ss >= 0 && ss < 60)
                    return new TimeOnly(hh, mm, ss);
            }
        }
        if (digitsOnly.Length == 4)
        {
            if (int.TryParse(digitsOnly[..2], out var hh2)
                && int.TryParse(digitsOnly.Substring(2, 2), out var mm2))
            {
                if (hh2 >= 0 && hh2 < 24 && mm2 >= 0 && mm2 < 60)
                    return new TimeOnly(hh2, mm2, 0);
            }
        }

        var formats = new[] { "HHmmss", "HHmm", "H:mm:ss", "H:mm", "HH:mm:ss", "HH:mm" };
        if (TimeOnly.TryParseExact(s, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var tt))
            return tt;

        if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return TimeOnly.FromDateTime(dt);

        return null;
    }

    // ---------- shared helpers ----------
    public static string Normalize(string s)
    {
        if (s == null) return string.Empty;
        s = s.Trim().Trim(new char[] { '"', '\'', '\uFEFF', '\u200B' });
        s = ToHalfWidthDigits(s);
        s = Regex.Replace(s, @"\s+", " ");
        return s;
    }

    public static string ToHalfWidthDigits(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        var sb = new StringBuilder(input.Length);
        foreach (var ch in input)
        {
            if (ch >= '０' && ch <= '９')
            {
                sb.Append((char)('0' + (ch - '０')));
            }
            else
            {
                sb.Append(ch);
            }
        }
        return sb.ToString();
    }

    public static int RocToAd(int rocYear)
    {
        if (rocYear >= 1911) return rocYear;
        return 1911 + rocYear;
    }

    public static bool IsValidYmd(int y, int m, int d)
    {
        if (y < 1900 || y > 9999) return false;
        if (m < 1 || m > 12) return false;
        if (d < 1 || d > DateTime.DaysInMonth(y, m)) return false;
        return true;
    }

    public static bool TryParseDateWithCommonFormats(string s, out DateOnly result)
    {
        var formats = new[]
        {
                "yyyy/MM/dd","yyyy/M/d","yyyy-MM-dd","yyyy-M-d","yyyy.MM.dd","yyyy.M.d","yyyyMMdd"
            };
        return DateOnly.TryParseExact(s, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
    }

    // 你的其他 helper（保留）
    public static short? TryParseShort(string? input) => short.TryParse(input?.Trim(), out var s) ? s : null;
    public static sbyte? TryParseSByte(string? input) => sbyte.TryParse(input?.Trim(), out var s) ? s : null;
    public static decimal? TryParseDecimal(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        if (decimal.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
            return Math.Round(d, 8);
        return null;
    }

    public static string? SafeTrim(string? input, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(input)) return null;
        input = input.Trim();
        return input.Length > maxLength ? input[..maxLength] : input;
    }
    /// <summary>
    /// 將民國格式日期（例如 0114-11-03 或 114-11-03）轉為西元 DateOnly
    /// </summary>
    public static DateOnly? ParseTaiwanDate(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        text = text.Trim();

        // 格式如：0114-11-03、114-11-03、114/11/03、114.11.03
        var match = System.Text.RegularExpressions.Regex.Match(
            text,
            @"0?(?<y>\d{3,4})[-/.年](?<m>\d{1,2})[-/.月](?<d>\d{1,2})"
        );

        if (!match.Success)
            return null;

        if (!int.TryParse(match.Groups["y"].Value, out int rocYear))
            return null;

        int year = rocYear + 1911;
        if (!int.TryParse(match.Groups["m"].Value, out int month)) return null;
        if (!int.TryParse(match.Groups["d"].Value, out int day)) return null;

        try
        {
            return new DateOnly(year, month, day);
        }
        catch
        {
            return null;
        }
    }
    /// <summary>
    /// HTML 日期字串 → DateOnly（自動判斷民國／西元）
    /// </summary>
    public static DateOnly? ParseDate(HtmlNodeCollection cols, int index)
    {
        if (cols.Count <= index)
            return null;

        var text = cols[index].InnerText.Trim();
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var rocPattern = @"^0?\d{3}[-/.年]";
        if (System.Text.RegularExpressions.Regex.IsMatch(text, rocPattern))
        {
            var match = System.Text.RegularExpressions.Regex.Match(
                text,
                @"0?(?<y>\d{3,4})[-/.年](?<m>\d{1,2})[-/.月]?(?<d>\d{1,2})"
            );

            if (match.Success &&
                int.TryParse(match.Groups["y"].Value, out int rocYear) &&
                int.TryParse(match.Groups["m"].Value, out int month) &&
                int.TryParse(match.Groups["d"].Value, out int day))
            {
                try
                {
                    int year = rocYear + 1911;
                    return new DateOnly(year, month, day);
                }
                catch
                {
                    return null;
                }
            }
        }

        if (DateTime.TryParse(text, out var dt))
            return DateOnly.FromDateTime(dt);

        return null;
    }
}
