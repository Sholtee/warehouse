using Microsoft.AspNetCore.Http;

namespace Warehouse.Core.Exceptions
{
    public sealed class NotFoundException : RequestException
    {
        public override int HttpStatus { get; } = StatusCodes.Status404NotFound;
    }
}
