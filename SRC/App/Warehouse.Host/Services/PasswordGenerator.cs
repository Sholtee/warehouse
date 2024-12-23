/********************************************************************************
* PasswordGenerator.cs                                                          *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;
using System.Collections.Generic;

namespace Warehouse.Host.Services
{
    using Core.Abstractions;
    using Core.Extensions;

    internal sealed class PasswordGenerator: IPasswordGenerator
    {
        public string Generate(int minLength)
        {
            const string
                UPPER_CASE_LETTERS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ",
                LOWER_CASE_LETTERS = "abcdefghijklmnopqrstuvwxyz",
                NUMBERS = "0123456789",
                SPECIALS = "!@#$%^&*()_+";

            List<char> chars = [];

            int partLen = (int) Math.Ceiling((double) minLength / 4);

            chars.AddRange(UPPER_CASE_LETTERS.Random(partLen));
            chars.AddRange(LOWER_CASE_LETTERS.Random(partLen));
            chars.AddRange(NUMBERS.Random(partLen));
            chars.AddRange(SPECIALS.Random(partLen));

            return string.Join("", chars.Shuffle());
        }
    }
}
