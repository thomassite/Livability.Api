using Livability.Api.Models;
using Livability.Api.Models.NpaTma;
using Livability.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Livability.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NpaTmaController : ControllerBase
    {
        private readonly NpaTmaImportService _importService;
        private readonly ILogger<NpaTmaController> _logger;

        public NpaTmaController(NpaTmaImportService importService, ILogger<NpaTmaController> logger)
        {
            _importService = importService;
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
                resp.Result = await _importService.NpaTmaNearby(request);
                resp.Success = true;
            }
            catch(Exception ex)
            {
                resp.Success = false;
                resp.Message = ex.Message;
            }

            return resp;
        }
        [RequestSizeLimit(200_000_000)] // 200 MB
        [HttpPost("import")]
        [RequestFormLimits(MultipartBodyLengthLimit = 200_000_000)]
        public async Task<IActionResult> ImportCsv(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("請上傳 CSV 檔案。");

            try
            {
                var count = await _importService.ImportFromCsvAsync(file.OpenReadStream());
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
