using System;
using System.Collections.Generic;

namespace MoonTweaks.Scripting;

/// <summary>Where in a script a call came from, so failures name a line an author can open.</summary>
public readonly record struct ScriptOrigin(string File, int Line)
{
    /// <summary>Origin used when no script is responsible, such as an internal call.</summary>
    public static readonly ScriptOrigin None = new("<host>", 0);

    /// <inheritdoc/>
    public override string ToString() => Line > 0 ? $"{File}:{Line}" : File;
}

/// <summary>A failure attributable to a script, carrying the line that caused it.</summary>
public sealed class ScriptError(ScriptOrigin origin, string message)
    : Exception($"{origin}: {message}")
{
    /// <summary>Where the failure happened.</summary>
    public ScriptOrigin Origin { get; } = origin;
}

/// <summary>One function scripts can call, already reduced to neutral values.</summary>
public sealed record FunctionBinding(
    string Name,
    Func<ScriptOrigin, IReadOnlyList<ScriptValue>, ScriptValue> Invoke);

/// <summary>A dotted path and the functions bound beneath it.</summary>
public sealed record ModuleBinding(string Path, IReadOnlyList<FunctionBinding> Functions);

/// <summary>A script to run, named so that errors can point back at it.</summary>
public sealed record ScriptFile(string Name, string Source);

/// <summary>
/// Runs scripts against a set of bindings. The only place an interpreter appears;
/// swapping engines means implementing this and nothing else.
/// </summary>
public interface IScriptHost : IDisposable
{
    /// <summary>Exposes a module to every script this host runs.</summary>
    void Bind(ModuleBinding module);

    /// <summary>Runs one script, throwing <see cref="ScriptError"/> if it fails.</summary>
    void Run(ScriptFile file);
}
