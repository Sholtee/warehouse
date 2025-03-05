/********************************************************************************
* ValidatorTests.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

using NUnit.Framework;

namespace Warehouse.API.Tests
{
    using Controllers;

    [TestFixture]
    internal sealed class ValidatorTests
    {
        [Test]
        public void ListProductOverviewsParamValidator_NoError()
        {
            ListProductOverviewsParam param = new()
            {
                Filter = new()
                {
                    Block = new()
                    {
                        String = new()
                        {
                            Comparison = StringComparisonType.Equals,
                            Value = "Samsung",
                            Property = nameof(ProductOverview.Brand)
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
                                Property = nameof(ProductOverview.Brand)
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

            ValidationContext context = new(param);
            List<ValidationResult> results = [];
            
            Assert.That(Validator.TryValidateObject(param, context, results, true), Is.True);
        }

        [Test]
        public void ListProductOverviewsParamValidator_InvalidFilter_1()
        {
            ListProductOverviewsParam param = new()
            {
                Filter = new()
                {
                    And = new()
                    {
                        Decimal = new()
                        {
                            Comparison = StructComparisonType.LessThan,
                            Value = 1500,
                            Property = nameof(ProductOverview.Price)
                        }
                    },
                    Or = new()
                    {
                        String = new()
                        {
                            Comparison = StringComparisonType.Equals,
                            Value = "Sony",
                            Property = nameof(ProductOverview.Brand)
                        }
                    }
                }
            };

            ValidationContext context = new(param);
            List<ValidationResult> results = [];

            Assert.Multiple(() =>
            {
                Assert.That(Validator.TryValidateObject(param, context, results, true), Is.False);
                Assert.That(results, Has.Count.EqualTo(1));
                Assert.That(results[0].ToString(), Is.EqualTo("Validation for \"Filter\" failed!"));
            });
        }

        [Test]
        public void ListProductOverviewsParamValidator_InvalidFilter_2()
        {
            ListProductOverviewsParam param = new()
            {
                Filter = new()
                {
                    String = new()
                    {
                        Comparison = StringComparisonType.Equals,
                        Value = "Samsung",
                        Property = nameof(ProductOverview.Brand)
                    },
                    Decimal = new()
                    {
                        Comparison = StructComparisonType.LessThan,
                        Value = 1000,
                        Property = nameof(ProductOverview.Price)
                    }
                },
            };

            ValidationContext context = new(param);
            List<ValidationResult> results = [];

            Assert.Multiple(() =>
            {
                Assert.That(Validator.TryValidateObject(param, context, results, true), Is.False);
                Assert.That(results, Has.Count.EqualTo(1));
                Assert.That(results[0].ToString(), Is.EqualTo("Validation for \"Filter\" failed!"));
            });
        }

        [Test]
        public void ListProductOverviewsParamValidator_InvalidFilter_3()
        {
            ListProductOverviewsParam param = new()
            {
                Filter = new()
                {
                    String = new()
                    {
                        Comparison = StringComparisonType.Equals,
                        Value = "Samsung",
                        Property = "Invalid"
                    }
                }
            };

            ValidationContext context = new(param);
            List<ValidationResult> results = [];

            Assert.Multiple(() =>
            {
                Assert.That(Validator.TryValidateObject(param, context, results, true), Is.False);
                Assert.That(results, Has.Count.EqualTo(1));
                Assert.That(results[0].ToString(), Is.EqualTo("Validation for \"Filter\" failed!"));
            });          
        }

        [Test]
        public void ListProductOverviewsParamValidator_InvalidSort()
        {
            ListProductOverviewsParam param = new()
            {
                Filter = new()
                {
                    String = new()
                    {
                        Comparison = StringComparisonType.Equals,
                        Value = "Samsung",
                        Property = nameof(ProductOverview.Brand)
                    }
                },
                SortBy = new()
                {
                    Properties =
                    [
                        new() 
                        {
                            Kind = SortKind.Ascending,
                            Property = "Invalid",
                        }
                    ]
                }
            };

            ValidationContext context = new(param);
            List<ValidationResult> results = [];

            Assert.Multiple(() =>
            {
                Assert.That(Validator.TryValidateObject(param, context, results, true), Is.False);
                Assert.That(results, Has.Count.EqualTo(1));
                Assert.That(results[0].ToString(), Is.EqualTo("Validation for \"SortBy\" failed!"));
            });
        }
    }
}
