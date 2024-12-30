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
using Swashbuckle.AspNetCore.Filters;

namespace Warehouse.API.Registrations
{
    /// <summary>
    /// Registrations
    /// </summary>
    public static class Registrations
    {
        /// <summary>
        /// Registers the controllers provided by this assembly.
        /// </summary>
        public static IMvcCoreBuilder AddControllers(this IMvcCoreBuilder mvcBuilder)
        {
            ArgumentNullException.ThrowIfNull(mvcBuilder, nameof(mvcBuilder));

            Assembly executingAsm = Assembly.GetExecutingAssembly();

            mvcBuilder.Services
                .AddAutoMapper(executingAsm)
                .AddSwaggerExamplesFromAssemblies(executingAsm);

            return mvcBuilder
                .AddApplicationPart(executingAsm)
                .AddControllersAsServices();
        }
    }
}
