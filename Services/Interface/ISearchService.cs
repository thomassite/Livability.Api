using Livability.Api.Models.Search;

namespace Livability.Api.Services.Interface
{
    public interface ISearchService
    {
        Task<NearByViewModel> NearBy(NearbyRequest request);
    }
}
