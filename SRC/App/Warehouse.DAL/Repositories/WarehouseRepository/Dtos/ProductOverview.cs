using System;

namespace Warehouse.DAL
{
    public class ProductOverview
    {
        public required Guid Id { get; init; }

        public required string MainImage { get; init; }

        public required string Name { get; init; }

        public required string Brand { get; init; }

        public required uint Quantity { get; init; }

        public required decimal Price { get; init; }

        public required DateTime ReleaseDate { get; init; }
    }
}
