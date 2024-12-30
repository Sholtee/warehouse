/********************************************************************************
* ListProductOverviewsParamExample.cs                                           *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using Swashbuckle.AspNetCore.Filters;

namespace Warehouse.API.Controllers
{
    internal sealed class ListProductOverviewsParamExample : IExamplesProvider<ListProductOverviewsParam>
    {
        public ListProductOverviewsParam GetExamples() => new()
        {
            Filter = new()
            {
                Block = new()
                {
                    String = new()
                    {
                        Comparison = StringComparisonType.Equals,
                        Value = "Samsung",
                        Property = nameof(ProductOverview.Name)
                    },
                    And = new()
                    {
                        Decimal = new()
                        {
                            Comparison = StructComparisonType.LessThan,
                            Value = 1000,
                            Property = nameof(ProductOverview.Price)
                        }
                    }
                },
                Or = new()
                {
                    Block = new()
                    {
                        String = new()
                        {
                            Comparison = StringComparisonType.Equals,
                            Value = "Sony",
                            Property = nameof(ProductOverview.Name)
                        },
                        And = new()
                        {
                            Decimal = new()
                            {
                                Comparison = StructComparisonType.LessThan,
                                Value = 1500,
                                Property = nameof(ProductOverview.Price)
                            }
                        }
                    }
                }
            },
            SortBy = new()
            {
                Properties =
                    [
                        new() { Property = nameof(ProductOverview.Name), Kind = SortKind.Ascending },
                        new() { Property = nameof(ProductOverview.Price), Kind = SortKind.Descending }
                    ]
            },
            Page = new()
            {
                Size = 5,
                Skip = 3
            }
        };
    }
}
