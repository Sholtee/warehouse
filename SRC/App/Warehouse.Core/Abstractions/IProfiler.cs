/********************************************************************************
* IProfiler.cs                                                                  *
*                                                                               *
* Author: Denes Solti                                                           *
* Project: Warehouse API (boilerplate)                                          *
* License: MIT                                                                  *
********************************************************************************/
using System;

namespace Warehouse.Core.Abstractions
{
    /// <summary>
    /// Contract on how to measure execution time in the call graph
    /// </summary>
    /// <remarks>Does nothing if the underlying profiler is not active.</remarks>
    public interface IProfiler
    {
        /// <summary>
        /// Extend the call graph.
        /// </summary>
        IDisposable? Step(string name);

        /// <summary>
        /// Custom timings for SQL scripts or REST API invocations.
        /// </summary>
        IDisposable? CustomTiming(string category, string commandString);

        /// <summary>
        /// Silences the profiler for the code in the using block
        /// </summary>
        IDisposable? Ignore();
    }
}
