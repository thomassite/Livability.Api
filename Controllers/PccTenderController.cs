using Livability.Api.Models;
using Livability.Api.Models.PccTender;
using Livability.Api.Services;
using Microsoft.AspNetCore.Mvc;

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
            var result = await _service.Crawler(request);
            return Resp.Ok(result);
        }
    }
}
