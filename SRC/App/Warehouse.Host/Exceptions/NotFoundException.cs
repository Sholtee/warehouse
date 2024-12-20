using Microsoft.AspNetCore.Http;

namespace Warehouse.API.Exceptions
{
    internal sealed class NotFoundException : RequestException
    {
        public override int HttpStatus { get; } = StatusCodes.Status404NotFound;
    }
}
