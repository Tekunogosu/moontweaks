using System;
using System.Collections.Generic;
using MoonTweaks.Api;
using MoonTweaks.Host;
using MoonTweaks.Scripting;

namespace MoonTweaks.LuaBench;

/// <summary>
/// What the layer between an engine and the game costs: reading the arguments a
/// script wrote into the shapes a binding takes, and writing the table a handler is
/// given.
/// </summary>
/// <remarks>
/// Measured apart from any engine, and once rather than per engine, because none of
/// it is an engine's doing — every interpreter reaches it through the same neutral
/// values. <see cref="Workload"/> builds its bindings by hand precisely so that the
/// cost of this layer cannot favour one engine over another, which left it measured
/// by nothing at all. A server crosses it on every call a script makes and again for
/// every event it raises, so it is worth a number.
///
/// Nothing here needs a running server. Reading a shape and writing a table are
/// questions about the shape rather than about the world it describes.
/// </remarks>
public static class Binder
{
    /// <summary>A module whose only purpose is to be bound and called.</summary>
    /// <remarks>
    /// Declared here rather than borrowed from the mod because every real domain
    /// reaches the game, and what is being measured is the crossing rather than
    /// whatever waits on the other side of it. The shapes it takes are the mod's own:
    /// an <see cref="AreaSpec"/> is what a script scanning for something around it
    /// fills in, over and over, which is the shape worth knowing the cost of.
    /// </remarks>
    [LuaModule("bench")]
    public sealed class Subject
    {
        /// <summary>Four plain numbers, which is the cheapest crossing there is.</summary>
        [LuaFunction("scalars")]
        public double Scalars(ScriptOrigin origin, double x, double y, double z, double range) =>
            x + y + z + range;

        /// <summary>One table, read into the shape a search takes.</summary>
        [LuaFunction("area")]
        public double Area(ScriptOrigin origin, AreaSpec area) => area.Range;
    }

    /// <summary>Runs every binder case, at the same scale the engine cases run at.</summary>
    public static IReadOnlyList<Timing> Measure(int scale)
    {
        var module = DomainBinder.Bind(new Subject());
        var scalars = Function(module, "scalars");
        var area = Function(module, "area");

        // Built once and reused: what is being measured is the crossing, and a script
        // that writes a table literal has already paid for the table by this point.
        var origin = new ScriptOrigin("bench.lua", 1);
        IReadOnlyList<ScriptValue> numbers =
            [Number(64), Number(96), Number(128), Number(8)];
        IReadOnlyList<ScriptValue> table =
        [
            new ScriptValue.Map(new Dictionary<string, ScriptValue>
            {
                ["x"] = Number(64),
                ["y"] = Number(96),
                ["z"] = Number(128),
                ["range"] = Number(8),
            }),
        ];
        var tick = new TimerPayload(0.05f);

        return
        [
            Timed("bind: 4 scalar args", "call", 2_000_000, scale,
                count => { for (var i = 0; i < count; i++) scalars.Invoke(origin, numbers); }),

            Timed("bind: one Area table", "call", 1_000_000, scale,
                count => { for (var i = 0; i < count; i++) area.Invoke(origin, table); }),

            Timed("write: event table", "table", 1_000_000, scale,
                count => { for (var i = 0; i < count; i++) PayloadWriter.Table(tick); }),
        ];
    }

    /// <summary>One case, at the count this run asked for.</summary>
    private static Timing Timed(
        string name, string unit, int iterations, int scale, Action<int> body) =>
        Measurement.Of(name, unit, Math.Max(1, iterations / scale), body);

    /// <summary>One bound function by the name it was bound under.</summary>
    private static FunctionBinding Function(ModuleBinding module, string name)
    {
        foreach (var function in module.Functions)
        {
            if (function.Name == name) return function;
        }

        throw new InvalidOperationException($"the bench subject binds no '{name}'");
    }

    private static ScriptValue Number(double value) => new ScriptValue.Num(value);
}
