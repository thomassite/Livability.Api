using AutoMapper;
using HtmlAgilityPack;
using Livability.Api.Context;
using Livability.Api.Dto;
using Livability.Api.Models.PccTender;
using Microsoft.EntityFrameworkCore;
using Microsoft.Playwright;
using System.Net;
using System.Text;

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

            _logger.LogInformation("🚀 開始爬取採購網資料（timeRange={timeRange}）", request.timeRange);

            try
            {
                var htmlPages = await FetchPccHtmlPagesAsync(new DateTime(2025, 10, 9), new DateTime(2025,10,9));
                int totalCount = 0;

                foreach (var html in htmlPages)
                {
                    var count = await ParseAndSaveTenderHtml(html);
                    totalCount += count;
                    _logger.LogInformation("✅ 本頁新增 {Count} 筆，累計 {Total} 筆", count, totalCount);
                }

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

            var resultHeader = doc.DocumentNode.SelectSingleNode("//div[contains(@class, 'title_1s') and contains(., '查詢結果')]");
            if (resultHeader == null)
            {
                _logger.LogWarning("⚠️ 找不到『查詢結果』區塊。頁面結構可能已變更。");
                return 0;
            }

            var table = resultHeader.SelectSingleNode("following::table[1]");
            if (table == null)
            {
                _logger.LogWarning("⚠️ 找不到查詢結果下的表格。");
                return 0;
            }

            var rows = table.SelectNodes(".//tr");
            if (rows == null || rows.Count <= 1)
            {
                _logger.LogWarning("⚠️ 查詢結果表格中沒有資料列。");
                return 0;
            }

            var agencies = await _db.PccAgencies.ToListAsync();
            var tenders = await _db.PccTenderMains.ToListAsync();

            int count = 0;

            foreach (var row in rows.Skip(1)) // 跳過表頭
            {
                var cols = row.SelectNodes("td");
                if (cols == null || cols.Count < 4)
                    continue;

                string SafeText(HtmlNode n) =>
                    WebUtility.HtmlDecode(n.InnerText.Trim().Replace("\n", "").Replace("\r", "").Replace("&nbsp;", ""));
                //採購性質
                var category = SafeText(cols[5]);
                //機關名稱
                var agencyName = SafeText(cols[1]);
                // 標案案號與名稱
                var linkNode = cols[3].SelectSingleNode(".//a");
                var tenderCaseNo = "";
                var tenderName = "";
                var detailUrl = "";
                var budgetAmount = SafeText(cols[8]);
                var tpamPk = "";
                if (linkNode != null)
                {
                    detailUrl = linkNode.GetAttributeValue("href", "").Trim();
                    if (!string.IsNullOrEmpty(detailUrl) && !detailUrl.StartsWith("http"))
                        detailUrl = $"https://web.pcc.gov.tw{detailUrl}";

                    var caseText = linkNode.InnerText.Trim();
                    var lines = caseText.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if (lines.Length > 0)
                        tenderCaseNo = lines[0];
                    if (lines.Length > 1)
                        tenderName = lines[1];
                }
                else
                {
                    _logger.LogError($"linkNode error {SafeText(cols[3])}");
                    continue;
                }

                // 日期欄位
                DateOnly? noticeDate = ParseDate(cols, 6);
                DateOnly? bidDeadline = ParseDate(cols, 7);

                // 重複檢查
                bool exists = tenders.Any(t =>
                    t.Category == category &&
                    t.TenderCaseNo == tenderCaseNo &&
                    agencies.Any(a => a.Id == t.PpcAgencyId && a.AgencyName == agencyName)
                );

                if (exists)
                {
                    _logger.LogInformation("↩️ 已存在，略過：{0} | {1} | {2}", category, agencyName, tenderCaseNo);
                    continue;
                }

                // 機關
                var agency = agencies.FirstOrDefault(a => a.AgencyName == agencyName);
                if (agency == null)
                {
                    var newAgency = new PccAgency { AgencyName = agencyName };
                    agency = (await _db.PccAgencies.AddAsync(newAgency)).Entity;
                    await _db.SaveChangesAsync();
                    agencies.Add(agency);
                }

                // 新增標案
                var pccTenderMain = new PccTenderMain
                {
                    Category = category,
                    PpcAgencyId = agency.Id,
                    TenderCaseNo = tenderCaseNo,
                    TenderName = tenderName,
                    NoticeDate = noticeDate,
                    BidDeadline = bidDeadline,
                    DetailUrl = detailUrl
                };

                await _db.PccTenderMains.AddAsync(pccTenderMain);
                tenders.Add(pccTenderMain);
                count++;

                _logger.LogInformation("[{0}] 新增標案：{1} | {2} | {3}", count, agencyName, tenderCaseNo, tenderName);
            }

            await _db.SaveChangesAsync();
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

        /// <summary>
        /// 使用 Playwright 依序抓取所有分頁 HTML
        /// </summary>
        public async Task<List<string>> FetchPccHtmlPagesAsync(DateTime startDate, DateTime endDate)
        {
            var htmlPages = new List<string>();
            var random = new Random();

            string[] userAgents =
            {
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125 Safari/537.36",
        "Mozilla/5.0 (Macintosh; Intel Mac OS X 13_4) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.4 Safari/605.1.15",
        "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123 Safari/537.36",
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:126.0) Gecko/20100101 Firefox/126.0"
    };
            var userAgent = userAgents[random.Next(userAgents.Length)];

            using var playwright = await Playwright.CreateAsync();
            var browser = await playwright.Chromium.LaunchAsync(new()
            {
                Headless = true,
                SlowMo = random.Next(30, 80),
                Args = new[] { "--disable-blink-features=AutomationControlled", "--no-sandbox", "--disable-gpu" }
            });

            var context = await browser.NewContextAsync(new()
            {
                UserAgent = userAgent,
                ViewportSize = new ViewportSize { Width = 1366, Height = 768 },
                IgnoreHTTPSErrors = true
            });

            var page = await context.NewPageAsync();
            _logger.LogInformation("🌐 開始建立 session...");

            // 進入查詢頁
            await page.GotoAsync("https://web.pcc.gov.tw/prkms/tender/common/proctrg/readTenderProctrg",
                new() { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 60000 });

            await Task.Delay(random.Next(1500, 3000));

            // ======  標案種類：招標 ======
            await page.CheckAsync("#level_6");
            _logger.LogInformation("✅ 已選擇『招標』");

            // ======  公告類型：各式招標公告 ======
            await page.SelectOptionAsync("#declarationSelect", "TENDER_WAY_ALL_DECLARATION");

            // ======  標的分類：不限 ======
            await page.CheckAsync("#RadProctrgCate4");
            _logger.LogInformation("✅ 已選擇『標的分類：不限』");

            // ======  公告日期：日期區間 ======
            await page.CheckAsync("#level_23");
            _logger.LogInformation("✅ 已切換至『公告日期區間』模式");

            // 轉換日期 → 民國年格式（例：114/10/09）
            string ToROC(DateTime dt) => $"{dt.Year - 1911}/{dt.Month:D2}/{dt.Day:D2}";
            var rocStart = ToROC(startDate);
            var rocEnd = ToROC(endDate);

            _logger.LogInformation("🗓️ 設定查詢日期區間：{Start} - {End}", rocStart, rocEnd);

            // 勾選日期區間
            await page.CheckAsync("#level_23");

            // 🧩 用 JS 設定隱藏欄位值 + 觸發事件
            await page.EvaluateAsync($@"
    const s = document.querySelector('#tenderStartDate');
    const e = document.querySelector('#tenderEndDate');
    if (s) {{
        s.value = '{rocStart}';
        s.dispatchEvent(new Event('input', {{ bubbles: true }}));
        s.dispatchEvent(new Event('change', {{ bubbles: true }}));
    }}
    if (e) {{
        e.value = '{rocEnd}';
        e.dispatchEvent(new Event('input', {{ bubbles: true }}));
        e.dispatchEvent(new Event('change', {{ bubbles: true }}));
    }}
");
            _logger.LogInformation("📅 已設定查詢日期區間 {Start} 至 {End}", rocStart, rocEnd);

            await Task.Delay(random.Next(500, 1200));

            // ======  送出查詢 ======
            await page.WaitForFunctionAsync("typeof proctrgTenderSearch === 'function'");
            await page.EvaluateAsync("proctrgTenderSearch()");
            // 等主容器存在
            await page.WaitForSelectorAsync("#printArea", new() { Timeout = 60000 });

            // 再等表格或資料列出現（tpam 是主表格 ID）
            await page.WaitForSelectorAsync("table#tpam tr", new() { Timeout = 60000 });

            _logger.LogInformation("🚀 查詢執行中...");

            int pageIndex = 1;
            while (true)
            {
                var html = await page.ContentAsync();
                htmlPages.Add(html);
                _logger.LogInformation("📄 已擷取第 {PageIndex} 頁", pageIndex);

                await Task.Delay(random.Next(2000, 4000));

                var nextButton = await page.QuerySelectorAsync("#pagelinks a:has-text('下一頁')");
                if (nextButton == null)
                {
                    _logger.LogInformation("🚫 沒有下一頁，結束。");
                    break;
                }

                var currentUrl = page.Url;
                await nextButton.ClickAsync();

                try
                {
                    await page.WaitForURLAsync(u => u != currentUrl, new() { Timeout = 8000 });
                }
                catch (TimeoutException)
                {
                    await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                }

                await page.WaitForSelectorAsync("div.title_1s", new() { Timeout = 60000 });
                pageIndex++;

                if (pageIndex % 5 == 0)
                {
                    int pause = random.Next(10000, 20000);
                    _logger.LogInformation("⏸ 模擬使用者休息 {Pause} ms...", pause);
                    await Task.Delay(pause);
                }
            }

            await browser.CloseAsync();
            _logger.LogInformation("✅ 完成，共擷取 {Count} 頁。", htmlPages.Count);

            return htmlPages;
        }
    }
}
