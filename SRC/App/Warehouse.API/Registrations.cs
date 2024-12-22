/********************************************************************************
* Registrations.cs                                                              *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;
using System.Reflection;

using Microsoft.Extensions.DependencyInjection;

namespace Warehouse.API.Registrations
{
    public static class Registrations
    {
        public static IMvcCoreBuilder AddControllers(this IMvcCoreBuilder mvcBuilder)
        {
            ArgumentNullException.ThrowIfNull(mvcBuilder, nameof(mvcBuilder));

            Assembly executingAsm = Assembly.GetExecutingAssembly();

            mvcBuilder.Services.AddAutoMapper(executingAsm);

            return mvcBuilder
                .AddApplicationPart(executingAsm)
                .AddControllersAsServices();
        }
    }
}
