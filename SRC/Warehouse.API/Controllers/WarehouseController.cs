using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Warehouse.API.Controllers
{
    using Dtos;
    using Infrastructure.Auth;

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

        [HttpPost("products")]
        [ProducesResponseType(typeof(List<Product>), StatusCodes.Status200OK)]
        [BasicAuthorize(Roles.User)]
        public async Task<IActionResult> ListProducts([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Disallow)] ProductFilter filter, CancellationToken cancellationToken = default)
        {
            
        }

        [HttpGet("product/{id}")]
        [ProducesResponseType(typeof(ProductWithDetails), StatusCodes.Status200OK), ProducesResponseType(StatusCodes.Status404NotFound)]
        [BasicAuthorize(Roles.User)]
        public async Task<IActionResult> GetProductWithDetailsAsync([FromRoute] int id, CancellationToken cancellationToken = default)
        {
            //
            // TODO: implement
            //

            return Ok(new ProductWithDetails
            {
                Name = "Samsung Galaxy Tab A9+",
                Types = ["tablet"],
                State = ProductState.New,
                Description = "The Samsung Galaxy Tab A9 is a budget Android tablet computer and part of the Samsung Galaxy Tab series designed by Samsung Electronics.",
                Quantity = 10,
                Price = 10000
            });
        }
    }
}
