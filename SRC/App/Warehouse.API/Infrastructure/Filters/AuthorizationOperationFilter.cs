using System.Reflection;

using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Warehouse.API.Infrastructure.Filters
{
    using Attributes;

    internal sealed class AuthorizationOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            ApiExplorerAuthorizationAttribute? authorizationAttribute = context
                .MethodInfo
                .DeclaringType!
                .GetCustomAttributes<ApiExplorerAuthorizationAttribute>(true)
                .Union
                (
                    context
                        .MethodInfo
                        .GetCustomAttributes<ApiExplorerAuthorizationAttribute>(true)
                )
                .SingleOrDefault();
            if (authorizationAttribute is null)
            {
                operation.Security.Clear();
                return;
            }

            operation.Security = [authorizationAttribute.SecurityRequirement];
        }
    }
}
