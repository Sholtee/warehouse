using System.ComponentModel.DataAnnotations;

namespace Warehouse.API.Dtos
{
    public class ProductFilter
    {
        public string? NameLike { get; init; }
        public ProductState? State { get; init; }
        public decimal? PriceOver { get; init; }
        public decimal? PriceUnder { get; init; }
        public uint? SkipPages { get; init; } = 0;
        [Range(1, 50)]
        public uint? PageSize { get; init; } = 10;
    }
}
