using CsvHelper;
using Livability.Api.Context;
using Livability.Api.Dto;
using Livability.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Formats.Asn1;
using System.Globalization;

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

        [HttpPost("import")]
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
