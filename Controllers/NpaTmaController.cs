using Livability.Api.Models;
using Livability.Api.Models.NpaTma;
using Livability.Api.Services;
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
        private readonly NpaTmaService _service;
        private readonly ILogger<NpaTmaController> _logger;

        public NpaTmaController(NpaTmaService service, ILogger<NpaTmaController> logger)
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
            RespModel<List<NpaTmaLocationViewModel>> resp = new RespModel<List<NpaTmaLocationViewModel>>();
            try
            {
                resp.Result = await _service.NpaTmaNearby(request);
                resp.Success = true;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "❌ 查詢失敗: {Error}", ex.Message);
                resp.Success = false;
                resp.Message = ex.Message;
            }

            return resp;
        }

        /// <summary>
        /// A1 & A2 交通事故資料匯入
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        [RequestSizeLimit(200_000_000)] // 200 MB
        [HttpPost("import")]
        [RequestFormLimits(MultipartBodyLengthLimit = 200_000_000)]
        public async Task<IActionResult> ImportCsv(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("請上傳 CSV 檔案。");

            try
            {
                var count = await _service.ImportFromCsvAsync(file.OpenReadStream());
                return Ok(new { message = $"成功匯入 {count} 筆交通事故資料。" });
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException?.Message ?? ex.Message;
                _logger.LogError(ex, "❌ 匯入失敗: {Error}", inner);
                return StatusCode(500, new
                {
                    message = "匯入失敗",
                    error = inner
                });
            }
        }
    }
}
