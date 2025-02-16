/********************************************************************************
* ResponseCodeAttribute.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;

using Microsoft.AspNetCore.Mvc.Filters;

namespace Warehouse.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ResponseCodeAttribute(int statusCode) : Attribute, IResultFilter
    {
        public void OnResultExecuted(ResultExecutedContext context)
        {
            ArgumentNullException.ThrowIfNull(context, nameof(context));

            if (context.Exception is null && !context.Canceled)
            {
                context.HttpContext.Response.StatusCode = statusCode;
            }
        }

        public void OnResultExecuting(ResultExecutingContext context) {}
    }
}
