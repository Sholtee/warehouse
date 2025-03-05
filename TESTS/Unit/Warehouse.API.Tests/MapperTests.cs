/********************************************************************************
* MapperTests.cs                                                                *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using AutoMapper;
using NUnit.Framework;

namespace Warehouse.API.Tests
{
    using Controllers;
    using Controllers.Profiles;

    using static Controllers.ListProductOverviewsParam;

    [TestFixture]
    internal sealed class MapperTests
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
            };

            Expression<Func<DAL.ProductOverview, bool>> mapped = mapper.Map<Expression<Func<DAL.ProductOverview, bool>>>(filter);
            Assert.That(mapped.ToString(), Is.EqualTo("productOverview => (((productOverview.Name == \"Samsung\") AndAlso (productOverview.Price < 1000)) OrElse ((productOverview.Name == \"Sony\") AndAlso (productOverview.Price < 1500)))"));
        }

        [Test]
        public void SortByMapper_ShouldMap()
        {
            Mapper mapper = new
            (
                new MapperConfiguration(cfg =>
                {
                    cfg.AddProfile<SortByProfile>();
                })
            );

            SortBy<SortProperties> sortBy = new()
            {
                Properties =
                [
                    new() { Property = nameof(ProductOverview.Name), Kind = SortKind.Ascending },
                    new() { Property = nameof(ProductOverview.Price), Kind = SortKind.Descending }
                ]
            };

            List<(Expression<Func<DAL.ProductOverview, object>> Property, bool Asc)> mapped = mapper.Map<List<(Expression<Func<DAL.ProductOverview, object>> Property, bool Asc)>>(sortBy);

            Assert.That
            (
                mapped.Select(prop => prop.ToString()),
                Is.EquivalentTo
                ([
                    "(productOverview => Convert(productOverview.Name, Object), True)",
                    "(productOverview => Convert(productOverview.Price, Object), False)"
                ])
            );      
        }

        [Test]
        public void ListProductOverviewsParamMapper_ShouldMap()
        {
            Mapper mapper = new
            (
                new MapperConfiguration(cfg =>
                {
                    cfg.AddProfile<FilterProfile>();
                    cfg.AddProfile<SortByProfile>();
                    cfg.AddProfile<ListProductOverviewsParamProfile>();
                })
            );

            DAL.ListProductOverviewsParam mapped = mapper.Map<DAL.ListProductOverviewsParam>(new ListProductOverviewsParamExample().GetExamples());

            Assert.Multiple(() =>
            {
                Assert.That(mapped.Filter, Is.Not.Null);
                Assert.That(mapped.SortBy, Has.Count.EqualTo(2));
                Assert.That(mapped.Skip, Is.EqualTo(0));
                Assert.That(mapped.Take, Is.EqualTo(5));
            });
        }
    }
}
