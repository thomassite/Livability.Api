namespace Livability.Api.Models.PccTender
{
    public class CrawlerRequest
    {
        /// <summary>
        /// 年度 ex: 114
        /// </summary>
        public int timeRange { get; set; }
        /// <summary>
        /// 招標檢索
        /// </summary>
        public string[] querySentence { get; set; } = new string[] { "道路", "橋梁", "捷運", "污水", "排水", "管線", "建設", "工程", "新建", "整建", "擴建", "改善", "下水道", "停車場", "社會住宅", "公共設施", "堤防" };
    }
}
