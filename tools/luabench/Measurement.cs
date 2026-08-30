using System;
using System.Diagnostics;

namespace MoonTweaks.LuaBench;

/// <summary>
/// How anything here is timed. Sole owner of that, so a row describing an engine and
/// a row describing the binding layer were arrived at the same way and can be read
/// beside each other.
/// </summary>
public static class Measurement
{
    /// <summary>
    /// Operations to run before timing anything, however short the run asked for is.
    /// Below roughly this many the figures are the runtime compiling itself.
    /// </summary>
    private const int WARMUP_FLOOR = 200_000;

    /// <summary>Times one case, reporting what one operation cost and what it allocated.</summary>
    /// <remarks>
    /// The body runs once at a fraction of the count before it is measured, so what
    /// is timed is compiled code rather than the runtime still compiling it. A first
    /// run reads several times slow for that reason and says nothing about the subject.
    /// </remarks>
    public static Timing Of(string name, string unit, int iterations, Action<int> body)
    {
        // A fixed floor rather than a fraction of the count: a small run would
        // otherwise warm up proportionally less and report the runtime still
        // compiling itself as though that were the subject.
        body(Math.Max(iterations / 10, Math.Min(iterations, WARMUP_FLOOR)));

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        var clock = Stopwatch.StartNew();
        body(iterations);
        clock.Stop();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        return new Timing(name, unit, iterations,
            clock.Elapsed.TotalMilliseconds * 1_000_000.0 / iterations, (double)allocated / iterations);
    }
}
