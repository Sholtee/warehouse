using System;
using System.Linq.Expressions;

using AutoMapper;
using NUnit.Framework;

namespace Warehouse.API.Mappers.Tests
{
    using Controllers;
    using Controllers.Profiles;

    using static Controllers.ListProductOverviewsParam;

    [TestFixture]
    internal class MappersTests
    {
        [Test]
        public void FilterMapper_ShouldMap()
        {
            Mapper mapper = new
            (
                new MapperConfiguration(cfg =>
                {
                    cfg.AddProfile<FilterProfile>();
                })
            );

            Filter<IntFilter, DecimalFilter, DateFilter, StringFilter> filter = new()
            {
                Block = new()
                {
                    String = new()
                    {
                        Comparison = StringComparisonType.Equals,
                        Value = "Samsung",
                        Property = "Name"                       
                    },
                    And = new()
                    {
                        Decimal = new()
                        {
                            Comparison = StructComparisonType.LessThan,
                            Value = 1000,
                            Property = "Price"
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
                            Property = "Name"
                        },
                        And = new()
                        {
                            Decimal = new()
                            {
                                Comparison = StructComparisonType.LessThan,
                                Value = 1500,
                                Property = "Price"
                            }
                        }
                    }
                }
            };

            Assert.That(filter.Validate(null!), Is.Empty);

            Expression<Func<DAL.ProductOverview, bool>> mapped = mapper.Map<Expression<Func<DAL.ProductOverview, bool>>>(filter);
            Assert.That(mapped.ToString(), Is.EqualTo("productOverview => (((productOverview.Name == \"Samsung\") And (productOverview.Price < 1000)) Or ((productOverview.Name == \"Sony\") And (productOverview.Price < 1500)))"));
        }
    }
}
