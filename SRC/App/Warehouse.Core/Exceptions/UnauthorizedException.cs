/********************************************************************************
* UnauthorizedException.cs                                                      *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using Microsoft.AspNetCore.Http;

namespace Warehouse.Core.Exceptions
{
    public sealed class UnauthorizedException : RequestException
    {
        public override int HttpStatus { get; } = StatusCodes.Status401Unauthorized;

        public string? Authenticate { get; init; }

        public override void PrepareResponse(HttpResponse response)
        {
            base.PrepareResponse(response);
            
            if (!string.IsNullOrEmpty(Authenticate))
                response.Headers.Append("WWW-Authenticate", Authenticate);
        }
    }
}
