namespace Warehouse.API.Dtos
{
    /// <summary>
    /// Describes a product overview
    /// </summary>
    public class ProductOverview
    {
        /// <summary>
        /// The name of the product
        /// </summary>
        public required string Name { get; init; }

        /// <summary>
        /// The condition of the product
        /// </summary>
        public required ProductCondition Condition { get; init; }
        
        /// <summary>
        /// Available quantity
        /// </summary>
        public required uint Quantity { get; init; }

        /// <summary>
        /// Price
        /// </summary>
        public required decimal Price { get; init; }
    }
}
