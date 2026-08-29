using System;
using System.Collections.Generic;
using System.Linq;

namespace MoonTweaks.Scripting;

/// <summary>
/// The interpreters this mod can run scripts on, and the only place one is built.
/// Every engine implements <see cref="IScriptHost"/> and nothing else, so what a
/// script can do is the same whichever is running it and the difference between them
/// is only what it costs.
/// </summary>
/// <remarks>
/// One engine is registered. The shape is kept because it is what made replacing the
/// last one cheap: <c>scripts/bench.sh</c> measures whatever is registered here, and
/// a candidate is added beside the current engine and compared before it replaces it.
/// </remarks>
public static class ScriptEngine
{
    /// <summary>
    /// Engine a server runs on. A candidate replaces this only once it has been
    /// measured against it on the bindings a server actually uses, which is what
    /// <c>scripts/bench.sh</c> is for.
    /// </summary>
    public const string Default = LuaCSharpHost.EngineName;

    private static readonly Dictionary<string, Func<IScriptHost>> Engines =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [LuaCSharpHost.EngineName] = () => new LuaCSharpHost(),
        };

    /// <summary>
    /// Every engine that can be named, the default first and the rest after it. That
    /// order is what a report compares against, so the engine a server actually runs
    /// is the one another is measured relative to.
    /// </summary>
    public static IReadOnlyList<string> Names { get; } =
        [Default, .. Engines.Keys.Where(name => name != Default).OrderBy(name => name)];

    /// <summary>Whether an engine of this name exists, for validating a settings file.</summary>
    public static bool Knows(string name) => Engines.ContainsKey(name);

    /// <summary>Builds a fresh interpreter of the named engine.</summary>
    /// <exception cref="ArgumentException">The name is not one this mod offers.</exception>
    public static IScriptHost Create(string name) =>
        Engines.TryGetValue(name, out var build)
            ? build()
            : throw new ArgumentException(
                $"no script engine named '{name}'; this mod offers {string.Join(", ", Names)}", nameof(name));
}
