/********************************************************************************
* CustomModelDocumentFilter.cs                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Warehouse.Host.Infrastructure.Filters
{
    internal sealed class CustomModelDocumentFilter<T> : IDocumentFilter where T : class
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context) => context
            .SchemaGenerator
            .GenerateSchema(typeof(T), context.SchemaRepository);
    }
}
