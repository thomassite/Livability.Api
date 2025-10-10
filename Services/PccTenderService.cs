using AutoMapper;
using HtmlAgilityPack;
using Livability.Api.Context;
using Livability.Api.Dto;
using Livability.Api.Models.PccTender;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Livability.Api.Services
{
    public class PccTenderService : BaseService
    {

        public PccTenderService(LivabilityContext db, IMapper mapper, ILogger<PccTenderService> logger) : base(db, mapper, logger)
        {
        }

        /// <summary>
        /// 主流程：爬取所有頁面並入庫
        /// </summary>
        public async Task<int> Crawler(CrawlerRequest request)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);


            try
            {
                int totalCount = await FetchPccHtmlPagesAsync(request.startDate, request.endDate);

                _logger.LogInformation("🎯 全部頁面處理完成，共新增 {Count} 筆資料。", totalCount);
                return totalCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 爬取採購網時發生錯誤");
                return 0;
            }
        }

        /// <summary>
        /// 解析 HTML 表格內容並寫入 DB
        /// </summary>
        private async Task<int> ParseAndSaveTenderHtml(string html)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var table = doc.DocumentNode.SelectSingleNode("//table[@id='tpam' and contains(@class, 'tb_01')]");
            if (table == null)
            {
                _logger.LogWarning("⚠️ 找不到查詢結果表格。");
                return 0;
            }

            var rows = table.SelectNodes(".//tr");
            if (rows == null || rows.Count <= 1)
            {
                _logger.LogWarning("⚠️ 查詢結果表格中沒有資料列。");
                return 0;
            }

            // 📦 快取既有資料
            var existingAgencies = await _db.PccAgencies.AsNoTracking().ToListAsync();
            var existingPk = await _db.PccTenderMains.AsNoTracking().Select(t => t.TpamPk).ToListAsync();

            var newAgencies = new List<PccAgency>();
            var allAgencyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            int count = 0;

            // 🌀 [第一階段] 掃一遍收集所有機關名稱
            foreach (var row in rows.Skip(1))
            {
                var cols = row.SelectNodes("td");
                if (cols == null || cols.Count < 9)
                    continue;

                string SafeText(HtmlNode n) =>
                    WebUtility.HtmlDecode(n.InnerText.Trim().Replace("\n", "").Replace("\r", "").Replace("&nbsp;", ""));

                var agencyName = SafeText(cols[1]);
                if (string.IsNullOrWhiteSpace(agencyName))
                    continue;

                if (!existingAgencies.Any(a => a.AgencyName == agencyName) &&
                    !newAgencies.Any(a => a.AgencyName == agencyName) &&
                    allAgencyNames.Add(agencyName))
                {
                    newAgencies.Add(new PccAgency { AgencyName = agencyName });
                }
            }

            // 🏦 寫入所有新機關，並更新快取
            if (newAgencies.Any())
            {
                await _db.PccAgencies.AddRangeAsync(newAgencies);
                await _db.SaveChangesAsync();
                existingAgencies.AddRange(newAgencies);
                _logger.LogInformation("🏢 新增 {0} 個新機關。", newAgencies.Count);
            }

            var newTenders = new List<PccTenderMain>();

            // 🌀 [第二階段] 掃描每筆標案
            foreach (var row in rows.Skip(1))
            {
                try
                {
                    var cols = row.SelectNodes("td");
                    if (cols == null || cols.Count < 9)
                        continue;

                    string SafeText(HtmlNode n) =>
                        WebUtility.HtmlDecode(n.InnerText.Trim().Replace("\n", "").Replace("\r", "").Replace("&nbsp;", ""));

                    var category = SafeText(cols[5]);
                    var agencyName = SafeText(cols[1]);
                    var budgetAmount = SafeText(cols[8]);
                    var tpamPk = "";
                    var tenderCaseNo = "";
                    var tenderName = "";
                    var detailUrl = "";

                    // 抓案號 (br 前的文字)
                    var tdRaw = cols[2].InnerHtml;
                    var beforeBr = tdRaw.Split("<br", StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                    if (!string.IsNullOrEmpty(beforeBr))
                    {
                        var textOnly = HtmlEntity.DeEntitize(Regex.Replace(beforeBr, "<.*?>", "")).Trim();
                        if (!string.IsNullOrEmpty(textOnly))
                            tenderCaseNo = textOnly;
                    }

                    // 名稱與連結
                    var linkNode = cols[2].SelectSingleNode(".//a");
                    if (linkNode != null)
                    {
                        detailUrl = linkNode.GetAttributeValue("href", "").Trim();
                        if (!string.IsNullOrEmpty(detailUrl))
                        {
                            if (!detailUrl.StartsWith("http"))
                                detailUrl = $"https://web.pcc.gov.tw{detailUrl}";

                            var matchPk = Regex.Match(detailUrl, @"pk=([^&]+)");
                            if (matchPk.Success)
                                tpamPk = matchPk.Groups[1].Value;
                        }

                        // 嘗試從 script 抓標案名稱
                        var scriptNode = linkNode.SelectSingleNode(".//script");
                        if (scriptNode != null)
                        {
                            var match = Regex.Match(scriptNode.InnerText, @"pageCode2Img\(""(?<name>[^""]+)""\)");
                            if (match.Success)
                                tenderName = match.Groups["name"].Value.Trim();
                        }

                        if (string.IsNullOrWhiteSpace(tenderName))
                            tenderName = WebUtility.HtmlDecode(linkNode.InnerText.Trim());
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ 無法找到標案連結：{0}", SafeText(cols[2]));
                        continue;
                    }

                    // 清理文字
                    tenderCaseNo = tenderCaseNo.Trim();
                    tenderName = tenderName.Trim();
                    decimal.TryParse(budgetAmount.Replace(",", ""), out var parsedBudget);

                    DateOnly? noticeDate = ParseDate(cols, 6);
                    DateOnly? bidDeadline = ParseDate(cols, 7);

                    if (string.IsNullOrWhiteSpace(tpamPk))
                        continue;
                    if (existingPk.Contains(tpamPk))
                        continue;

                    // 找出機關
                    var agency = existingAgencies.FirstOrDefault(a => a.AgencyName == agencyName);
                    if (agency == null)
                    {
                        _logger.LogWarning("⚠️ 無法對應機關：{0}", agencyName);
                        continue;
                    }

                    newTenders.Add(new PccTenderMain
                    {
                        Category = category,
                        TenderCaseNo = tenderCaseNo,
                        TenderName = tenderName,
                        NoticeDate = noticeDate,
                        BidDeadline = bidDeadline,
                        BudgetAmount = parsedBudget,
                        DetailUrl = detailUrl,
                        TpamPk = tpamPk,
                        PpcAgencyId = agency.Id
                    });

                    existingPk.Add(tpamPk);
                    count++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ 解析標案時發生錯誤。");
                }
            }

            // 🚀 寫入標案
            if (newTenders.Any())
            {
                await _db.PccTenderMains.AddRangeAsync(newTenders);
                await _db.SaveChangesAsync();
                _logger.LogInformation("✅ 成功新增 {0} 筆標案。", count);
            }
            else
            {
                _logger.LogInformation("⚙️ 沒有新增任何新標案。");
            }

            return count;
        }


        /// <summary>
        /// HTML 日期字串 → DateOnly（自動判斷民國／西元）
        /// </summary>
        private static DateOnly? ParseDate(HtmlNodeCollection cols, int index)
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

        public async Task<int> FetchPccHtmlPagesAsync(DateTime startDate, DateTime endDate)
        {
            var random = new Random();

            string[] userAgents =
            {
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125 Safari/537.36",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 13_4) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.4 Safari/605.1.15",
        "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123 Safari/537.36",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:126.0) Gecko/20100101 Firefox/126.0"
    };
            var userAgent = userAgents[random.Next(userAgents.Length)];

            using var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            using var client = new HttpClient(handler);

            client.DefaultRequestHeaders.Add("User-Agent", userAgent);
            client.DefaultRequestHeaders.Add("Accept-Language", "zh-TW,zh;q=0.9,en;q=0.8");
            client.DefaultRequestHeaders.Add("Referer", "https://web.pcc.gov.tw/prkms/tender/common/proctrg/indexTenderProctrg");
            client.DefaultRequestHeaders.Add("Connection", "keep-alive");

            var rocStart = $"{startDate.Year}/{startDate.Month:D2}/{startDate.Day:D2}";
            var rocEnd = $"{endDate.Year}/{endDate.Month:D2}/{endDate.Day:D2}";

            _logger.LogInformation("📅 查詢區間：{Start} - {End}", rocStart, rocEnd);

            // 🧠 URL 產生器
            string BuildUrl(int page) =>
                $"https://web.pcc.gov.tw/prkms/tender/common/proctrg/readTenderProctrg?" +
                $"pageSize=100&firstSearch=false&searchType=tpam&isBinding=N&isLogIn=N" +
                $"&level_1=on&tenderStatus=TENDER_STATUS_0&tenderWay=TENDER_WAY_ALL_DECLARATION" +
                $"&proctrgCode1=&proctrgCode2=&proctrgCode3=&radProctrgCate=&dateType=isDate" +  // ✅ 改為 isDate
                $"&tenderStartDate={WebUtility.UrlEncode(rocStart)}" +
                $"&tenderEndDate={WebUtility.UrlEncode(rocEnd)}" +
                $"&d-49738-p={page}";

            int pageIndex = 1;
            int totalCount = 0;
            while (true)
            {
                var url = BuildUrl(pageIndex);
                _logger.LogInformation("🌐 抓取第 {Page} 頁: {Url}", pageIndex, url);

                string html = string.Empty;
                int retry = 0;

                // --- ✅ 加入重試機制 ---
                while (retry < 3)
                {
                    try
                    {
                        html = await client.GetStringAsync(url);
                        break;
                    }
                    catch (Exception ex)
                    {
                        retry++;
                        _logger.LogWarning(ex, "⚠️ 第 {Page} 頁第 {Retry} 次重試中...", pageIndex, retry);
                        await Task.Delay(random.Next(3000, 7000));
                    }
                }

                if (string.IsNullOrEmpty(html))
                {
                    _logger.LogError("❌ 第 {Page} 頁多次重試失敗，中斷。", pageIndex);
                    break;
                }

                // --- ✅ 確認有資料表才收錄 ---
                var doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(html);

                var tableNode = doc.DocumentNode.SelectSingleNode("//table[@id='tpam' and contains(@class, 'tb_01')]");
                if (tableNode == null)
                {
                    _logger.LogWarning("⚠️ 第 {Page} 頁未發現資料表 (table#tpam)，可能查無資料或被導回首頁。", pageIndex);
                    break;
                }

                totalCount += await ParseAndSaveTenderHtml(html);

                // --- ✅ 用 XPath 判斷是否有「下一頁」連結 ---
                var nextNode = doc.DocumentNode.SelectSingleNode("//span[@id='pagelinks']//a[contains(text(), '下一頁')]");
                if (nextNode == null)
                {
                    _logger.LogInformation("🚫 沒有下一頁，結束。");
                    break;
                }

                // --- ✅ 取得下一頁的 href ---
                var href = nextNode.GetAttributeValue("href", null);
                if (string.IsNullOrEmpty(href))
                {
                    _logger.LogInformation("🚫 沒有下一頁的連結，結束。");
                    break;
                }

                // ✅ 檢查 d-49738-p 頁碼是否仍在遞增（避免死循環）
                if (href.Contains($"d-49738-p={pageIndex}"))
                {
                    _logger.LogWarning("⚠️ 下一頁頁碼未變動 ({Page})，可能遇到最後一頁。", pageIndex);
                    break;
                }

                pageIndex++;

                // --- ⏳ 模擬人類行為 ---
                int wait = random.Next(2000, 5000);
                _logger.LogInformation("⏸ 等待 {Wait} 毫秒以模擬人類操作", wait);
                await Task.Delay(wait);
            }

            return totalCount;
        }

    }
}
