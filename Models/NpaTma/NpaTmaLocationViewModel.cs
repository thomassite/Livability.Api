namespace Livability.Api.Models.NpaTma
{
    public class NpaTmaLocationViewModel
    {
        /// <summary>
        /// 自增主鍵，用於唯一識別每筆事故紀錄
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// 發生年度
        /// </summary>
        public short? Year { get; set; }

        /// <summary>
        /// 發生月份 (1~12)
        /// </summary>
        public sbyte? Month { get; set; }
        public string? AccidentType { get; set; }

        /// <summary>
        /// 事故經度座標
        /// </summary>
        public decimal? Longitude { get; set; }

        /// <summary>
        /// 事故緯度座標
        /// </summary>
        public decimal? Latitude { get; set; }
    }
}
