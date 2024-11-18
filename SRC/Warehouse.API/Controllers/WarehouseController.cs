using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Warehouse.API.Controllers
{
    using Auth;

    [ApiController, Produces("application/json"), Route("api/v1"), Authorize]
    public class WarehouseController(ILogger<WarehouseController> logger) : ControllerBase
    {
        [HttpGet("healthcheck"), ProducesResponseType(StatusCodes.Status204NoContent), AllowAnonymous]
        public async Task<IActionResult> HealthCheck(CancellationToken cancellationToken = default)
        {
            //
            // TODO: test DB connection
            //

            return NoContent();
        }

        [HttpGet("product/{id}"), ProducesResponseType(StatusCodes.Status404NotFound), BasicAuthorize(Roles.Admin)]
        public async Task<IActionResult> GetProductWithDetailsAsync(int id, CancellationToken cancellationToken = default)
        {
            //
            // TODO: implement
            //

            return Ok(new
            {
                Name = "Samsung Galaxy Tab A9+",
                Types = new string[] { "tablet" },
                State = "new",
                Description = "The Samsung Galaxy Tab A9 is a budget Android tablet computer and part of the Samsung Galaxy Tab series designed by Samsung Electronics.",
                Quantity = 10,
                Price = 10000
            });
        }
    }
}
