namespace Warehouse.API.Controllers.Dtos
{
    /// <summary>
    /// Product details
    /// </summary>
    public class ProductDetails : ProductOverview
    {
        /// <summary>
        /// Product types associated with this item.
        /// </summary>
        public required IReadOnlyList<string> Types { get; init; }

        /// <summary>
        /// Long description of the product.
        /// </summary>
        public required string Description { get; init; }
    }
}
