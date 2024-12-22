/********************************************************************************
* ApiExplorerAuthorizationAttribute.cs                                          *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;

using Microsoft.OpenApi.Models;

namespace Warehouse.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public abstract class ApiExplorerAuthorizationAttribute : Attribute
    {
        public abstract OpenApiSecurityRequirement SecurityRequirement { get; }
    }

    public sealed class ApiExplorerBasicAuthorizationAttribute : ApiExplorerAuthorizationAttribute
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

    public sealed class ApiExplorerSessionCookieAuthorizationAttribute : ApiExplorerAuthorizationAttribute
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
