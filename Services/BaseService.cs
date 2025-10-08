using AutoMapper;
using Livability.Api.Context;

namespace Livability.Api.Services
{
    public class BaseService
    {
        public readonly IMapper _mapper;
        public readonly LivabilityContext _db;
        public readonly ILogger _logger;

        public BaseService(LivabilityContext db, IMapper mapper, ILogger logger)
        {
            _mapper = mapper;
            _db = db;
            _logger = logger;
        }
    }
}
