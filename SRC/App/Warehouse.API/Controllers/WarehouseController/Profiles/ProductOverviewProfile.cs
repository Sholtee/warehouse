using AutoMapper;

namespace Warehouse.API.Controllers.Profiles
{
    internal sealed class ProductOverviewProfile : Profile
    {
        public ProductOverviewProfile()
        {
            CreateMap<DAL.ProductOverview, Controllers.ProductOverview>();
        }
    }
}
