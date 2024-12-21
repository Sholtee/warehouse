using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Warehouse.API.Controllers
{
    using Core.Attributes;
    using Core.Auth;
    using Core.Exceptions;
    using DAL;

    /// <summary>
    /// TODO: design
    /// </summary>
    [ApiController, Consumes("application/json"), Produces("application/json"), Route("api/v1"), Authorize, ApiExplorerSessionCookieAuthorization]
    public sealed class WarehouseController(IWarehouseRepository warehouseRepository) : ControllerBase
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
            if (!await warehouseRepository.IsHealthy())
                throw new Exception("Repo is not healthy");

            return NoContent();
        }

        /// <summary>
        /// Lists products matching on the given <paramref name="filter"/>.
        /// </summary>
        /// <param name="param">Parameter contining a the filter pattern.</param>
        /// <returns>Product list matching the given criteria.</returns>
        /// <response code="200">Returns the matching product list</response>
        /// <response code="400">The provided parameter is not in a valid form</response>
        /// <response code="403">The client is unathorized to execute the operation.</response>
        [HttpPost("products")]
        [RequiredRoles(Roles.User)]
        public Task<List<ListProductOverviewsResult>> ListProductOverviews([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Disallow)] ListProductOverviewsParam param)
        {
            //
            // TODO: implement
            //

            return Task.FromResult<List<ListProductOverviewsResult>>([]);
        }

        /// <summary>
        /// Gets a product associated with the given <paramref name="id"/>
        /// </summary>
        /// <param name="id">The product id</param>
        /// <returns>The product details.</returns>
        /// <response code="200">The product details</response>
        /// <response code="400">The provided <paramref name="id"/> is not in a valid form</response>
        /// <response code="403">The client is unathorized to execute the operation.</response>
        /// <response code="404">The provided <paramref name="id"/> is not a valid product id</response>
        [HttpGet("product/{id}")]
        [RequiredRoles(Roles.User)]
        public Task<GetProductDetailsResult> GetProductDetails([FromRoute] Guid id)
        {
            //
            // TODO: implement
            //

            if (id != Guid.Empty)
                throw new NotFoundException();

            return Task.FromResult
            (
                new GetProductDetailsResult
                {
                    ProductDetails = new ProductDetails
                    {
                        Id = id,
                        Brand = "Samsung",
                        Name = "Galaxy Tab A9+",
                        Types = ["tablet"],
                        Description = "The Samsung Galaxy Tab A9 is a budget Android tablet computer and part of the Samsung Galaxy Tab series designed by Samsung Electronics.",
                        Quantity = 10,
                        Price = 10000,
                        ReleaseDate = new DateTime(2023, 10, 17),
                        MainImage = "main.image",
                        Images = []
                    }
                }
            );
        }
    }
}
