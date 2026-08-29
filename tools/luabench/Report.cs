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
    private const int NameWidth = 34;

    /// <summary>Width of one engine's column.</summary>
    private const int EngineWidth = 20;

    /// <summary>Width of the column comparing two engines.</summary>
    private const int RatioWidth = 12;

    /// <summary>Writes the table a person reads.</summary>
    public static void Text(
        IReadOnlyList<EngineResult> engines, IReadOnlyList<Disagreement> disagreements, bool quick)
    {
        Console.WriteLine($"MoonTweaks script engines on .NET {Environment.Version}"
                          + (quick ? "  (--quick: counts divided by 20)" : ""));
        Console.WriteLine();

        WriteParity(engines, disagreements);
        Console.WriteLine();
        WriteCost(engines);
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

        Console.Write($"Cost{new string(' ', NameWidth - 4)}");
        foreach (var engine in engines) Console.Write(Centre(engine.Engine, EngineWidth));
        if (pair) Console.Write(Centre("speedup", RatioWidth));
        Console.WriteLine();

        Console.WriteLine(new string('-',
            NameWidth + EngineWidth * engines.Count + (pair ? RatioWidth : 0)));

        foreach (var subject in baseline.Timings)
        {
            Console.Write(Truncate(subject.Case, NameWidth).PadRight(NameWidth));

            foreach (var engine in engines)
            {
                var timing = engine.Timings.FirstOrDefault(other => other.Case == subject.Case);
                Console.Write(timing is null
                    ? "-".PadLeft(EngineWidth)
                    : $"{timing.Nanoseconds,10:F1} ns {timing.Bytes,6:F0} B".PadLeft(EngineWidth));
            }

            if (pair)
            {
                var other = engines[1].Timings.FirstOrDefault(entry => entry.Case == subject.Case);
                Console.Write(other is null || other.Nanoseconds <= 0
                    ? "".PadLeft(RatioWidth)
                    : $"{subject.Nanoseconds / other.Nanoseconds,8:F1}x".PadLeft(RatioWidth));
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
    public static void Json(IReadOnlyList<EngineResult> engines, IReadOnlyList<Disagreement> disagreements)
    {
        var document = new
        {
            runtime = Environment.Version.ToString(),
            agreed = !disagreements.Any(entry => entry.Unexplained),
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
                timings = engine.Timings.Select(timing => new
                {
                    @case = timing.Case,
                    unit = timing.Unit,
                    iterations = timing.Iterations,
                    nanoseconds = Math.Round(timing.Nanoseconds, 2),
                    bytes = Math.Round(timing.Bytes, 1),
                }),
            }),
        };

        Console.WriteLine(JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true }));
    }

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
