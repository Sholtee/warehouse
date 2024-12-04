using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Warehouse.API.Controllers
{
    using Dtos;
    using Exceptions;
    using Infrastructure.Auth;

    /// <summary>
    /// API endpoints.
    /// </summary>
    [ApiController, Produces("application/json"), Route("api/v1"), Authorize]
    public sealed class WarehouseController(ILogger<WarehouseController> logger) : ControllerBase
    {
        /// <summary>
        /// Healthcheck endpoint
        /// </summary>
        /// <returns>No content</returns>
        /// <response code="204">If the health check was successful</response>
        [HttpGet("healthcheck")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [AllowAnonymous]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> HealthCheck()
        {
            //
            // TODO: test DB connection
            //

            return NoContent();
        }

        /// <summary>
        /// Lists products matching on the given <paramref name="filter"/>.
        /// </summary>
        /// <param name="filter">Filter object</param>
        /// <returns>Product list matching the given criteria.</returns>
        /// <response code="200">Returns the list</response>
        [HttpPost("products")]
        [ProducesResponseType(typeof(List<ProductOverview>), StatusCodes.Status200OK)]
        [BasicAuthorize(Roles.User)]
        public async Task<IActionResult> ListProducts([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Disallow)] ProductFilter filter)
        {
            if (filter.PriceUnder < filter.PriceOver)
                throw new BadRequestException
                {
                    Errors = "Invalid price filter"
                };

            return Ok(new List<ProductOverview>());
        }

        /// <summary>
        /// Gets a product associated with the given <paramref name="id"/>
        /// </summary>
        /// <param name="id">The product id</param>
        /// <returns>Product list matching the given criteria.</returns>
        /// <response code="200">The prpduct details</response>
        /// <response code="404">The provided <paramref name="id"/> is not a valid product id</response>
        [HttpGet("product/{id}")]
        [ProducesResponseType(typeof(ProductDetails), StatusCodes.Status200OK), ProducesResponseType(StatusCodes.Status404NotFound)]
        [BasicAuthorize(Roles.User)]
        public async Task<IActionResult> GetProductWithDetailsAsync([FromRoute] int id)
        {
            if (id < 0)
                throw new BadRequestException
                {
                    Errors = new { id = "Value cannot be less then 0" }
                };

            //
            // TODO: implement
            //

            return Ok(new ProductDetails
            {
                Name = "Samsung Galaxy Tab A9+",
                Types = ["tablet"],
                Condition = ProductCondition.New,
                Description = "The Samsung Galaxy Tab A9 is a budget Android tablet computer and part of the Samsung Galaxy Tab series designed by Samsung Electronics.",
                Quantity = 10,
                Price = 10000
            });
        }
    }
}
