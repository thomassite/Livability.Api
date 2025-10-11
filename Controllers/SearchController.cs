using Livability.Api.Models;
using Livability.Api.Models.Search;
using Livability.Api.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace Livability.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly ISearchService _service;
        public SearchController(ISearchService service)
        {
            _service = service;
        }
        /// <summary>
        /// search
        /// </summary>
        [HttpGet("nearby")]
        public async Task<RespModel<NearByViewModel>> NearBy([FromQuery] NearbyRequest request)
        {
            var result = await _service.NearBy(request);
            return Resp.Ok(result);
        }
    }
}
