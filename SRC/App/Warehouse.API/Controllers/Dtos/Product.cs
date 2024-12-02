namespace Warehouse.API.Dtos
{
    public class Product
    {
        public required string Name { get; init; }
        public required ProductState State { get; init; }
        public required uint Quantity { get; init; }
        public required decimal Price { get; init; }
    }
}
