/********************************************************************************
* WarehouseController.cs                                                        *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.RateLimiting;

namespace Warehouse.API.Controllers
{
    using Core.Attributes;
    using Core.Auth;
    using Core.Exceptions;
    using DAL;

    /// <summary>
    /// TODO: finish implementation
    /// </summary>
    [ApiController, Consumes("application/json"), Produces("application/json"), Route("api/v1"), Authorize, EnableRateLimiting("userBound"), ApiExplorerSessionCookieAuthorization]
    public sealed class WarehouseController(IWarehouseRepository warehouseRepository, IMapper mapper) : ControllerBase
    {
        /// <summary>
        /// Health check endpoint
        /// </summary>
        /// <returns>No content</returns>
        /// <response code="204">If the health check was successful</response>
        [HttpGet("healthcheck"), ResponseCode(HttpStatusCode.NoContent)]
        [AllowAnonymous, DisableRateLimiting, ApiExplorerSettings(IgnoreApi = true)]
        public async Task HealthCheck()
        {
            if (!await warehouseRepository.IsHealthy())
                throw new InvalidOperationException("Repo is not healthy");

            //
            // TODO: other checks
            //
        }

        /// <summary>
        /// Lists products matching on the given <paramref name="param"/>.
        /// </summary>
        /// <param name="param">Parameter containing a the filter pattern.</param>
        /// <returns>Product list matching the given criteria.</returns>
        /// <response code="200">Returns the matching product list</response>
        /// <response code="400">The provided parameter is not in a valid form</response>
        /// <response code="403">The client is unauthorized to execute the operation.</response>
        [HttpPost("products")]
        [RequiredRoles(Roles.User)]
        public async Task<ListProductOverviewsResult> ListProductOverviews([FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Disallow)] ListProductOverviewsParam param) => new ListProductOverviewsResult
        {
            ProductOverviews = mapper.Map<List<ProductOverview>>
            (
                await warehouseRepository.ListProductOverviews
                (
                    mapper.Map<DAL.ListProductOverviewsParam>(param)
                )
            )
        };

        /// <summary>
        /// Gets a product associated with the given <paramref name="id"/>
        /// </summary>
        /// <param name="id" example="00000000-0000-0000-0000-000000000000">The product id</param>
        /// <returns>The product details.</returns>
        /// <response code="200">The product details</response>
        /// <response code="400">The provided <paramref name="id"/> is not in a valid form</response>
        /// <response code="403">The client is unauthorized to execute the operation.</response>
        /// <response code="404">The provided <paramref name="id"/> is not a valid product id</response>
        [HttpGet("product/{id}")]
        [RequiredRoles(Roles.User)]
        public async Task<GetProductDetailsByIdResult> GetProductDetailsById([FromRoute] Guid id) => new GetProductDetailsByIdResult
        {
            ProductDetails = mapper.Map<ProductDetails>
            (
                await warehouseRepository.GetProductDetailsById(id) ?? throw new NotFoundException()
            )
        };
    }
}
