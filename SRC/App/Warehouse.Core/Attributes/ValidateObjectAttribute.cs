/********************************************************************************
* ValidateObjectAttribute.cs                                                    *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;


namespace Warehouse.Core.Attributes
{
    public sealed class ValidateObjectAttribute(bool validateItems = false) : ValidationAttribute
    {
        private sealed class CompositeValidationResult(string message, List<ValidationResult> nestedResults) : ValidationResult(message), IEnumerable<ValidationResult>
        {
            public IEnumerator<ValidationResult> GetEnumerator() => nestedResults.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            ArgumentNullException.ThrowIfNull(validationContext, nameof(validationContext));

            if (value is not null)
            {
                List<ValidationResult> results = [];

                if (validateItems)
                {
                    foreach (object item in (IEnumerable)value)
                    {
                        if (item is not null)
                            Validator.TryValidateObject(item, new ValidationContext(item), results, true);
                    }
                }
                else
                    Validator.TryValidateObject(value, new ValidationContext(value), results, true);

                if (results.Count > 0)
                    return new CompositeValidationResult($"Validation for \"{validationContext.DisplayName}\" failed!", results);
            }

            return ValidationResult.Success;
        }
    }
}
