/********************************************************************************
* ProductOverviewProfile.cs                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
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
