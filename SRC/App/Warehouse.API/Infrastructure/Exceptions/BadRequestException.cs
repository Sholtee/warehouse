namespace Warehouse.API.Infrastructure.Exceptions
{
    internal sealed class BadRequestException : RequestException
    {
        public override int HttpStatus { get; } = StatusCodes.Status400BadRequest;
    }
}
