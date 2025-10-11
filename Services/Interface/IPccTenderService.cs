using Livability.Api.Models.PccTender;

namespace Livability.Api.Services.Interface
{
    public interface IPccTenderService
    {
        Task<int> Crawler(CrawlerRequest request);
    }
}
