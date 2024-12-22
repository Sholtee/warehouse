using AutoMapper;

namespace Warehouse.API.Controllers.Profiles
{
    internal sealed class ListProductOverviewsParamProfile : Profile
    {
        public ListProductOverviewsParamProfile()
        {
            CreateMap<Controllers.ListProductOverviewsParam, DAL.ListProductOverviewsParam>()
                .ForMember(static dst => dst.Skip, static opts => opts.MapFrom(src => src.Page.SkipPages * src.Page.PageSize))
                .ForMember(static dst => dst.Take, static opts => opts.MapFrom(src => src.Page.PageSize));
        }
    }
}
