using Microsoft.OpenApi.Models;

namespace Warehouse.API.Infrastructure.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    internal abstract class ApiExplorerAuthorizationAttribute : Attribute
    {
        public abstract OpenApiSecurityRequirement SecurityRequirement { get; }
    }

    internal sealed class ApiExplorerBasicAuthorizationAttribute : ApiExplorerAuthorizationAttribute
    {
        public override OpenApiSecurityRequirement SecurityRequirement { get; } = new OpenApiSecurityRequirement()
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Id = "basic",
                        Type = ReferenceType.SecurityScheme,
                    }
                },
                []
            }
        };
    }

    internal sealed class ApiExplorerSessionCookieAuthorizationAttribute : ApiExplorerAuthorizationAttribute
    {
        public override OpenApiSecurityRequirement SecurityRequirement { get; } = new OpenApiSecurityRequirement()
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Id = "session-cookie",
                        Type = ReferenceType.SecurityScheme,
                    }
                },
                []
            }
        };
    }
}
