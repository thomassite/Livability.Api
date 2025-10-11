using Livability.Api.Services.Interface;

namespace Livability.Api.Jobs
{

    /// <summary>
    /// 實際執行任務的 Job 類別
    /// </summary>
    public class MapGeocodeJob
    {
        private readonly IMapGeocodeService _service;

        public MapGeocodeJob(IMapGeocodeService service)
        {
            _service = service;
        }

        /// <summary>
        /// 執行地理位置補齊任務
        /// </summary>
        public async Task UpdateMissingAgenciesAsync()
        {
            await _service.BatchFillMissingAgenciesAsync();
        }
    }
}
