using Microsoft.AspNetCore.Http;

namespace Warehouse.API.Exceptions
{
    internal sealed class BadRequestException : RequestException
    {
        public override int HttpStatus { get; } = StatusCodes.Status400BadRequest;
    }
}
