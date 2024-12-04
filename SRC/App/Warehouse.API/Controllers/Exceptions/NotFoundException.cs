namespace Warehouse.API.Controllers.Exceptions
{
    internal sealed class NotFoundException : RequestException
    {
        public override int HttpStatus { get; } = StatusCodes.Status404NotFound;
    }
}
