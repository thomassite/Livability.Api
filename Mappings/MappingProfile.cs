using AutoMapper;
using Livability.Api.Dto;
using Livability.Api.Models.NpaTma;

namespace Livability.Api.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateMap<NpaTma, NpaTmaLocationViewModel>();
        }
    }
}
