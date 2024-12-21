using AutoMapper;

namespace Warehouse.API.Controllers.Profiles
{
    internal sealed class ProductDetailsProfile: Profile
    {
        public ProductDetailsProfile()
        {
            CreateMap<DAL.ProductDetails, Controllers.ProductDetails>();
        }
    }
}
