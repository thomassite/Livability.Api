namespace Livability.Api.Models.NpaTma
{
    public class NpaTmaLocationViewModel
    {
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
