using AutoMapper;

namespace Warehouse.API.Controllers.Profiles
{
    internal sealed class ListProductOverviewsParamProfile : Profile
    {
        public ListProductOverviewsParamProfile()
        {
            CreateMap<Controllers.ListProductOverviewsParam, DAL.ListProductOverviewsParam>()
                .ForMember(dst => dst.Skip, opts => opts.MapFrom(src => src.Page.SkipPages * src.Page.PageSize))
                .ForMember(dst => dst.Take, opts => opts.MapFrom(src => src.Page.PageSize));
        }
    }
}
