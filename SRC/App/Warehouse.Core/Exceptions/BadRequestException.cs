/********************************************************************************
* BadRequestException.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using Microsoft.AspNetCore.Http;

namespace Warehouse.Core.Exceptions
{
    public sealed class BadRequestException : RequestException
    {
        public override int HttpStatus { get; } = StatusCodes.Status400BadRequest;
    }
}
