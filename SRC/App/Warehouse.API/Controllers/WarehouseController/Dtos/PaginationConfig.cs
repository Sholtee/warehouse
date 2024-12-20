using System.ComponentModel.DataAnnotations;

namespace Warehouse.API.Controllers
{
    public abstract class PaginationConfig
    {
        /// <summary>
        /// Pages to skip.
        /// </summary>
        [Range(0, uint.MaxValue)]
        public uint? SkipPages { get; init; } = 0;

        /// <summary>
        /// Page size.
        /// </summary>
        [Range(1, 50)]
        public uint? PageSize { get; init; } = 10;
    }
}
