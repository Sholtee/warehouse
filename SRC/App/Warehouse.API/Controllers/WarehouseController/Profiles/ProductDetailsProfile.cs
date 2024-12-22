/********************************************************************************
* ProductDetailsProfile.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
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
