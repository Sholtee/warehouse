namespace Warehouse.API.Controllers.Exceptions
{
    internal sealed class BadRequestException : RequestException
    {
        public override int HttpStatus { get; } = StatusCodes.Status400BadRequest;
    }
}
