namespace Warehouse.API.Dtos
{
    public class ProductWithDetails: Product
    {
        public required IReadOnlyList<string> Types { get; init; }
        public required string Description { get; init; }
    }
}
