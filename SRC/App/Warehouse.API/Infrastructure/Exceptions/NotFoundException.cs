namespace Warehouse.API.Infrastructure.Exceptions
{
    internal sealed class NotFoundException : RequestException
    {
        public override int HttpStatus { get; } = StatusCodes.Status404NotFound;
    }
}
