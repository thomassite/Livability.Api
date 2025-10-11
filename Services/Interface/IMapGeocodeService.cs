namespace Livability.Api.Services.Interface
{
    public interface IMapGeocodeService
    {
        Task<(double? lat, double? lng, bool partialMatch)> GetCoordinatesAsync(string placeName);
        Task BatchFillMissingAgenciesAsync();
    }
}
