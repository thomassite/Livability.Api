using Livability.Api.Models.NpaTma;

namespace Livability.Api.Services.Interface
{
    public interface INpaTmaService
    {
        Task<List<NpaTmaLocationViewModel>> NpaTmaNearby(NpaTmaNearbyRequest request);
        Task<int> ImportFromCsvAsync(Stream csvStream);

    }
}
