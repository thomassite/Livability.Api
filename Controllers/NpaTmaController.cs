using Livability.Api.Models;
using Livability.Api.Models.NpaTma;
using Livability.Api.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace Livability.Api.Controllers
{
    /// <summary>
    /// https://www.npa.gov.tw/ch/app/data/list?module=wg051&id=2177 警政署公開資料
    /// </summary>
    /// <remarks> A1 & A2 即時交通事故</remarks>
    [ApiController]
    [Route("api/[controller]")]
    public class NpaTmaController : ControllerBase
    {
        private readonly INpaTmaService _service;
        private readonly ILogger<NpaTmaController> _logger;

        public NpaTmaController(INpaTmaService service, ILogger<NpaTmaController> logger)
        {
            _service = service;
            _logger = logger;
        }
        /// <summary>
        /// 查詢指定座標周圍30公里內的事故資料
        /// </summary>
        [HttpGet("nearby")]
        public async Task<RespModel<List<NpaTmaLocationViewModel>>> NpaTmaNearby([FromQuery] NpaTmaNearbyRequest request)
        {
            var result = await _service.NpaTmaNearby(request);
            return Resp.Ok(result);
        }

        /// <summary>
        /// A1 & A2 交通事故資料匯入
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        [RequestSizeLimit(200_000_000)] // 200 MB
        [HttpPost("import")]
        [RequestFormLimits(MultipartBodyLengthLimit = 200_000_000)]
        public async Task<RespModel<int>> ImportCsv(IFormFile file)
        {
            var result = await _service.ImportFromCsvAsync(file.OpenReadStream());
            return Resp.Ok(result);
        }
    }
}
