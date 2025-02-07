/********************************************************************************
* Profiler.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;

using StackExchange.Profiling;

namespace Warehouse.Host.Services
{
    using Core.Abstractions;

    internal sealed class Profiler(MiniProfiler core) : IProfiler
    {
        public IDisposable CustomTiming(string category, string commandString) => core.CustomTiming(category, commandString);

        public IDisposable Ignore() => core.Ignore();

        public IDisposable Step(string name) => core.Step(name);
    }
}
