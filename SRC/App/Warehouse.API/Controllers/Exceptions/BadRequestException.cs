namespace Warehouse.API.Controllers.Exceptions
{
    public class BadRequestException : RequestException
    {
        public override int HttpStatus { get; } = StatusCodes.Status400BadRequest;
    }
}
