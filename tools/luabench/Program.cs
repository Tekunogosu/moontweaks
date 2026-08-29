using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using MoonTweaks.Scripting;

namespace MoonTweaks.LuaBench;

/// <summary>
/// Runs the same Lua against every engine this mod offers and reports what each one
/// costs, having first recorded what it makes of the Lua the checks put through it.
/// With more than one registered, those recordings become a comparison and a
/// disagreement fails the run.
/// </summary>
/// <remarks>
/// The workload reaches an engine only through <see cref="IScriptHost"/>, which is
/// the whole of what an engine has to implement, so nothing here can favour one by
/// touching it more directly than another. Nothing in it needs a running server:
/// this measures the scripting layer, and the game is not part of that.
/// </remarks>
public static class Program
{
    /// <summary>Exit code for a run with nothing to report against it.</summary>
    private const int Agreed = 0;

    /// <summary>
    /// Exit code for a run where two engines read the same Lua differently, or where
    /// an engine was named that does not exist.
    /// </summary>
    private const int Disagreed = 1;

    /// <summary>
    /// Operations to run before timing anything, however short the run asked for is.
    /// Below roughly this many the figures are the runtime compiling itself.
    /// </summary>
    private const int WarmupFloor = 200_000;

    /// <summary>Runs the benchmark and reports it.</summary>
    public static int Main(string[] arguments)
    {
        if (arguments.Contains("--help") || arguments.Contains("-h"))
        {
            Console.WriteLine(Usage);
            return Agreed;
        }

        var asJson = arguments.Contains("--json");
        var quick = arguments.Contains("--quick");
        var names = EnginesNamed(arguments);

        if (names.Count == 0)
        {
            Console.Error.WriteLine($"no such engine; this mod offers {string.Join(", ", ScriptEngine.Names)}");
            return Disagreed;
        }

        var scale = quick ? 20 : 1;
        var engines = names.Select(name => Measure(name, scale)).ToList();
        var disagreements = Disagreements(engines);

        if (asJson) Report.Json(engines, disagreements);
        else Report.Text(engines, disagreements, quick);

        return disagreements.Any(entry => entry.Unexplained) ? Disagreed : Agreed;
    }

    private const string Usage = """
        luabench - measure the interpreter MoonTweaks runs scripts on

        usage: luabench [--engine NAME]... [--quick] [--json]

          --engine NAME  measure only this engine; repeatable (default: all)
          --quick        divide every iteration count by 20, for a fast check
          --json         emit the results as JSON instead of a table

        One engine is registered, so the checks are recorded rather than compared.
        Register a candidate beside it and this becomes the comparison that decides
        whether to swap: it exits 1 when two engines record different values for a
        check that does not already name a reason they would. A timing is never a
        reason to fail: an engine that is fast and wrong is not a candidate.
        """;

    /// <summary>Engines the arguments asked for, or all of them.</summary>
    private static IReadOnlyList<string> EnginesNamed(string[] arguments)
    {
        var chosen = new List<string>();

        for (var i = 0; i < arguments.Length - 1; i++)
        {
            if (arguments[i] != "--engine") continue;
            if (ScriptEngine.Knows(arguments[i + 1])) chosen.Add(arguments[i + 1]);
            else return [];
        }

        return chosen.Count > 0 ? chosen : ScriptEngine.Names;
    }

    /// <summary>Runs every check and every case against one engine.</summary>
    private static EngineResult Measure(string name, int scale)
    {
        var recorder = new Recorder();
        using var host = ScriptEngine.Create(name);
        host.Bind(Workload.ModuleFor(recorder));

        var checks = new Dictionary<string, string>();
        foreach (var check in Workload.Checks)
        {
            recorder.Recorded = ScriptValue.Nil.Instance;
            try
            {
                host.Run(check.File);
                checks[check.Name] = Render(recorder.Recorded);
            }
            catch (ScriptError failure)
            {
                checks[check.Name] = $"<error: {failure.Message}>";
            }
        }

        var timings = new List<Timing>();
        foreach (var subject in Workload.Cases)
        {
            var iterations = Math.Max(1, subject.Iterations / scale);
            timings.Add(Time(subject.Name, subject.Unit, iterations,
                count => host.Run(subject.FileFor(count))));
        }

        // The one case the host drives rather than the script: an event handler is
        // the host calling into Lua, and no chunk can time that from the inside.
        host.Run(Workload.HandlerFile);
        if (recorder.Handler is { } handler)
        {
            var iterations = Math.Max(1, 500_000 / scale);
            timings.Add(Time("handler: host calls script", "call", iterations, count =>
            {
                for (var i = 0; i < count; i++) handler.Call([Workload.HandlerPayload]);
            }));
        }

        return new EngineResult(name, checks, timings);
    }

    /// <summary>
    /// Times one case, reporting what one operation cost and what it allocated.
    /// </summary>
    /// <remarks>
    /// The body runs once at a fraction of the count before it is measured, so what
    /// is timed is compiled code rather than the runtime still compiling it. A first
    /// run reads several times slow for that reason and says nothing about an engine.
    /// </remarks>
    private static Timing Time(string name, string unit, int iterations, Action<int> body)
    {
        // A fixed floor rather than a fraction of the count: a small run would
        // otherwise warm up proportionally less and report the runtime still
        // compiling itself as though that were the engine.
        body(Math.Max(iterations / 10, Math.Min(iterations, WarmupFloor)));

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

    /// <summary>
    /// Every check where the engines did not all record the same value. Comparing
    /// rendered values rather than the values themselves is deliberate: what matters
    /// is that a script author cannot tell the engines apart, and the rendering is
    /// what an author would see.
    /// </summary>
    private static IReadOnlyList<Disagreement> Disagreements(IReadOnlyList<EngineResult> engines)
    {
        if (engines.Count < 2) return [];

        var found = new List<Disagreement>();

        foreach (var check in Workload.Checks)
        {
            var recorded = engines.ToDictionary(
                engine => engine.Engine,
                engine => engine.Checks.GetValueOrDefault(check.Name, "<not run>"));

            var differ = recorded.Values.Distinct().Count() > 1;

            // A check that names a reason and no longer needs one is reported as
            // well: the note is then describing an engine nobody is running.
            if (differ || check.Known is not null)
            {
                found.Add(new Disagreement(check.Name, recorded, differ, check.Known));
            }
        }

        return found;
    }

    /// <summary>
    /// One value as an author would see it, written so that two engines that mean the
    /// same thing render the same text: map keys are ordered, which the engines'
    /// own iteration order is not.
    /// </summary>
    public static string Render(ScriptValue value) => value switch
    {
        ScriptValue.Str text => $"\"{text.Value}\"",
        ScriptValue.Num number => number.Value.ToString("R", CultureInfo.InvariantCulture),
        ScriptValue.Bool flag => flag.Value ? "true" : "false",
        ScriptValue.List list => "[" + string.Join(", ", list.Items.Select(Render)) + "]",
        ScriptValue.Map map => "{" + string.Join(", ", map.Entries
            .OrderBy(entry => entry.Key, StringComparer.Ordinal)
            .Select(entry => $"{entry.Key}={Render(entry.Value)}")) + "}",
        ScriptValue.Func => "function",
        _ => "nil",
    };
}

/// <summary>What one case cost on one engine.</summary>
/// <param name="Case">The case's name.</param>
/// <param name="Unit">What one operation is.</param>
/// <param name="Iterations">How many operations were timed.</param>
/// <param name="Nanoseconds">Time one operation took.</param>
/// <param name="Bytes">Managed memory one operation allocated.</param>
public sealed record Timing(string Case, string Unit, int Iterations, double Nanoseconds, double Bytes);

/// <summary>Everything one engine recorded and everything it cost.</summary>
/// <param name="Engine">The engine's name.</param>
/// <param name="Checks">What it recorded for each parity check.</param>
/// <param name="Timings">What each case cost.</param>
public sealed record EngineResult(string Engine, IReadOnlyDictionary<string, string> Checks, IReadOnlyList<Timing> Timings);

/// <summary>One check worth reporting: the engines differed, or were expected to.</summary>
/// <param name="Check">The check's name.</param>
/// <param name="Recorded">What each engine recorded.</param>
/// <param name="Differed">Whether the engines actually recorded different values.</param>
/// <param name="Known">Why they were expected to, or null where they were not.</param>
public sealed record Disagreement(
    string Check, IReadOnlyDictionary<string, string> Recorded, bool Differed, string? Known)
{
    /// <summary>A difference nothing accounts for, which is the one that fails a run.</summary>
    public bool Unexplained => Differed && Known is null;

    /// <summary>A reason that no longer describes anything, which wants deleting.</summary>
    public bool Stale => !Differed && Known is not null;
}
