using AutoMapper;
using Livability.Api.Context;
using Livability.Api.Models.Search;
using Livability.Api.Services.Interface;

namespace Livability.Api.Services
{
    public class SearchService : BaseService, ISearchService
    {
        private readonly IMapGeocodeService _geocodeService;

        public SearchService(LivabilityContext db, IMapper mapper, ILogger<SearchService> logger, IMapGeocodeService geocodeService) : base(db, mapper, logger)
        {
            _geocodeService = geocodeService;
        }

        public async Task<NearByViewModel> NearBy(NearbyRequest request)
        {
            var googleResult = await _geocodeService.GetCoordinatesAsync(request.address);
            return new NearByViewModel
            {
                lat = googleResult.lat,
                lng = googleResult.lng
            };
        }
    }
}
