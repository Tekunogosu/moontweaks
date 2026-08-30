using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace MoonTweaks.LuaBench;

/// <summary>
/// How a run is written out. A table for someone reading it and JSON for something
/// else reading it, from the same results, so the two cannot say different things.
/// </summary>
public static class Report
{
    /// <summary>Width of the case-name column, wide enough for the longest case.</summary>
    private const int NAME_WIDTH = 34;

    /// <summary>Width of one engine's column.</summary>
    private const int ENGINE_WIDTH = 20;

    /// <summary>Width of the column comparing two engines.</summary>
    private const int RATIO_WIDTH = 12;

    /// <summary>Writes the table a person reads.</summary>
    public static void Text(
        IReadOnlyList<EngineResult> engines,
        IReadOnlyList<Timing> binder,
        IReadOnlyList<Disagreement> disagreements,
        bool quick)
    {
        Console.WriteLine($"MoonTweaks scripting on .NET {Environment.Version}"
                          + (quick ? "  (--quick: counts divided by 20)" : ""));

        if (quick)
        {
            // Learned the hard way: a quick row can read five times slow, which is
            // enough to invent a regression that is not there or hide one that is.
            Console.WriteLine();
            Console.WriteLine("Quick counts are too low for the runtime to finish optimising, so every");
            Console.WriteLine("figure below reads slow. Compare a quick run against another quick run;");
            Console.WriteLine("never against a full one.");
        }

        Console.WriteLine();

        WriteParity(engines, disagreements);
        Console.WriteLine();
        WriteCost(engines);
        Console.WriteLine();
        WriteBinder(binder);
    }

    /// <summary>
    /// Writes what the layer above the engine costs. One column rather than one per
    /// engine, because every engine reaches it through the same neutral values and
    /// none of them can make it cost anything different.
    /// </summary>
    private static void WriteBinder(IReadOnlyList<Timing> binder)
    {
        Console.Write($"Binder{new string(' ', NAME_WIDTH - 6)}");
        Console.WriteLine(Centre("all engines", ENGINE_WIDTH));
        Console.WriteLine(new string('-', NAME_WIDTH + ENGINE_WIDTH));

        foreach (var subject in binder)
        {
            Console.Write(Truncate(subject.Case, NAME_WIDTH).PadRight(NAME_WIDTH));
            Console.WriteLine($"{subject.Nanoseconds,10:F1} ns {subject.Bytes,6:F0} B".PadLeft(ENGINE_WIDTH));
        }

        Console.WriteLine();
        Console.WriteLine("A bind is one crossing from a script into a binding; a write is one table");
        Console.WriteLine("handed to a handler, which a server does once per event and once per timer tick.");
    }

    /// <summary>Writes what the engines agreed and disagreed about.</summary>
    private static void WriteParity(
        IReadOnlyList<EngineResult> engines, IReadOnlyList<Disagreement> disagreements)
    {
        var checks = Workload.Checks.Count;

        if (engines.Count < 2)
        {
            Console.WriteLine($"Parity  {checks} check(s) recorded; nothing to compare against with one engine.");
            return;
        }

        var unexplained = disagreements.Where(entry => entry.Unexplained).ToList();
        var expected = disagreements.Where(entry => entry.Differed && entry.Known is not null).ToList();
        var stale = disagreements.Where(entry => entry.Stale).ToList();

        Console.WriteLine($"Parity  {checks} check(s) across {engines.Count} engines: "
                          + $"{unexplained.Count} unexplained, {expected.Count} expected, "
                          + $"{checks - unexplained.Count - expected.Count} agreed.");

        Write("Unexplained differences - these are what this run is looking for", unexplained);
        Write("Expected differences - each names why, and none fails the run", expected);
        Write("Stale notes - these name a difference that is no longer there", stale);
    }

    /// <summary>One section of the parity report, written only when it has anything in it.</summary>
    private static void Write(string heading, IReadOnlyList<Disagreement> entries)
    {
        if (entries.Count == 0) return;

        Console.WriteLine();
        Console.WriteLine($"  {heading}:");

        foreach (var entry in entries)
        {
            Console.WriteLine();
            Console.WriteLine($"    {entry.Check}");
            foreach (var (engine, value) in entry.Recorded) Console.WriteLine($"      {engine,-12} {value}");
            if (entry.Known is not null) Console.WriteLine($"      why          {entry.Known}");
        }
    }

    /// <summary>Writes what each case cost on each engine, and how they compare.</summary>
    private static void WriteCost(IReadOnlyList<EngineResult> engines)
    {
        var baseline = engines[0];

        // A ratio is between two things. With more engines on the table the raw
        // figures are what compares them, and a single column could only pick a pair.
        var pair = engines.Count == 2;

        Console.Write($"Cost{new string(' ', NAME_WIDTH - 4)}");
        foreach (var engine in engines) Console.Write(Centre(engine.Engine, ENGINE_WIDTH));
        if (pair) Console.Write(Centre("speedup", RATIO_WIDTH));
        Console.WriteLine();

        Console.WriteLine(new string('-',
            NAME_WIDTH + ENGINE_WIDTH * engines.Count + (pair ? RATIO_WIDTH : 0)));

        foreach (var subject in baseline.Timings)
        {
            Console.Write(Truncate(subject.Case, NAME_WIDTH).PadRight(NAME_WIDTH));

            foreach (var engine in engines)
            {
                var timing = engine.Timings.FirstOrDefault(other => other.Case == subject.Case);
                Console.Write(timing is null
                    ? "-".PadLeft(ENGINE_WIDTH)
                    : $"{timing.Nanoseconds,10:F1} ns {timing.Bytes,6:F0} B".PadLeft(ENGINE_WIDTH));
            }

            if (pair)
            {
                var other = engines[1].Timings.FirstOrDefault(entry => entry.Case == subject.Case);
                Console.Write(other is null || other.Nanoseconds <= 0
                    ? "".PadLeft(RATIO_WIDTH)
                    : $"{subject.Nanoseconds / other.Nanoseconds,8:F1}x".PadLeft(RATIO_WIDTH));
            }

            Console.WriteLine();
        }

        Console.WriteLine();
        Console.WriteLine("ns is one operation of the kind the row names; "
                          + "B is managed memory allocated per operation.");

        if (pair)
        {
            Console.WriteLine($"speedup is {baseline.Engine} divided by {engines[1].Engine}: "
                              + $"above 1.0 means {engines[1].Engine} is that many times faster, "
                              + $"below 1.0 means it is slower.");
        }
    }

    /// <summary>Writes the same results for something other than a person to read.</summary>
    public static void Json(
        IReadOnlyList<EngineResult> engines,
        IReadOnlyList<Timing> binder,
        IReadOnlyList<Disagreement> disagreements)
    {
        var document = new
        {
            runtime = Environment.Version.ToString(),
            agreed = !disagreements.Any(entry => entry.Unexplained),
            binder = binder.Select(Written),
            disagreements = disagreements.Select(entry => new
            {
                check = entry.Check,
                recorded = entry.Recorded,
                differed = entry.Differed,
                known = entry.Known,
                unexplained = entry.Unexplained,
                stale = entry.Stale,
            }),
            engines = engines.Select(engine => new
            {
                name = engine.Engine,
                checks = engine.Checks,
                timings = engine.Timings.Select(Written),
            }),
        };

        Console.WriteLine(JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true }));
    }

    /// <summary>One timing as something other than a person reads it.</summary>
    /// <remarks>
    /// Sole owner of that shape, so a row measuring an engine and a row measuring the
    /// binder are written out the same way and read by the same code.
    /// </remarks>
    private static object Written(Timing timing) => new
    {
        @case = timing.Case,
        unit = timing.Unit,
        iterations = timing.Iterations,
        nanoseconds = Math.Round(timing.Nanoseconds, 2),
        bytes = Math.Round(timing.Bytes, 1),
    };

    /// <summary>A heading centred in its column.</summary>
    private static string Centre(string text, int width)
    {
        if (text.Length >= width) return text[..width];
        var left = (width - text.Length) / 2;
        return new string(' ', left) + text + new string(' ', width - text.Length - left);
    }

    /// <summary>A name cut to fit its column, with an ellipsis where it was cut.</summary>
    private static string Truncate(string text, int width) =>
        text.Length <= width ? text : text[..(width - 2)] + "..";
}
