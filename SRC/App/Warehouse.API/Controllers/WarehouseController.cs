using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Warehouse.API.Controllers
{
    using Dtos;
    using Infrastructure.Auth;
    using Infrastructure.Exceptions;

    /// <summary>
    /// API endpoints.
    /// </summary>
    [ApiController, Consumes("application/json"), Produces("application/json"), Route("api/v1"), Authorize]
    public sealed class WarehouseController(ILogger<WarehouseController> logger) : ControllerBase
    {
        /// <summary>
        /// Healthcheck endpoint
        /// </summary>
        /// <returns>No content</returns>
        /// <response code="204">If the health check was successful</response>
        [HttpGet("healthcheck")]
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
        /// <response code="400">The provided <paramref name="filter"/> is not in a valid form</response>
        /// <response code="401">The client is unathorized to execute the operation.</response>
        [HttpPost("products")]
        [RequiredRoles(Roles.User)]
        public async Task<List<ProductOverview>> ListProducts([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Disallow)] ProductFilter filter)
        {
            if (filter.PriceUnder < filter.PriceOver)
                throw new BadRequestException
                {
                    Errors = "Invalid price filter"
                };

            return [];
        }

        /// <summary>
        /// Gets a product associated with the given <paramref name="id"/>
        /// </summary>
        /// <param name="id">The product id</param>
        /// <returns>The product details./returns>
        /// <response code="200">The product details</response>
        /// <response code="400">The provided <paramref name="id"/> is not in a valid form</response>
        /// <response code="401">The client is unathorized to execute the operation.</response>
        /// <response code="404">The provided <paramref name="id"/> is not a valid product id</response>
        [HttpGet("product/{id}")]
        [RequiredRoles(Roles.User)]
        public async Task<ProductDetails> GetProductDetailsAsync([FromRoute] int id)
        {
            if (id < 0)
                throw new NotFoundException();

            //
            // TODO: implement
            //

            return new ProductDetails
            {
                Name = "Samsung Galaxy Tab A9+",
                Types = ["tablet"],
                Condition = ProductCondition.New,
                Description = "The Samsung Galaxy Tab A9 is a budget Android tablet computer and part of the Samsung Galaxy Tab series designed by Samsung Electronics.",
                Quantity = 10,
                Price = 10000
            };
        }
    }
}
