using Livability.Api.Models.NpaTma;
using Livability.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Livability.Api.Models.PccTender;
using Livability.Api.Services;

namespace Livability.Api.Controllers
{
    /// <summary>
    /// https://web.pcc.gov.tw/prkms/tender/common/bulletion/readBulletion 政府電子採購網
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PccTenderController : ControllerBase
    {
        private readonly PccTenderService _service;
        private readonly ILogger<PccTenderController> _logger;

        public PccTenderController(PccTenderService servivce,ILogger<PccTenderController> logger)
        {
            _service = servivce;
            _logger = logger;
        }


        [HttpPost("crawler")]
        public async Task<RespModel<int>> Crawler([FromBody] CrawlerRequest request)
        {
            RespModel<int> resp = new RespModel<int>();
            try
            {
                resp.Result = await _service.Crawler(request);
                resp.Success = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 執行失敗: {Error}", ex.Message);
                resp.Success = false;
                resp.Message = ex.Message;
            }

            return resp;
        }
    }
}
