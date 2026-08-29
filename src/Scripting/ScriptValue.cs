using System;
using System.Collections.Generic;

namespace MoonTweaks.Scripting;

/// <summary>
/// A Lua value in engine-neutral form. Everything above the scripting layer reads
/// these instead of the interpreter's own types, so the interpreter stays swappable.
/// </summary>
public abstract record ScriptValue
{
    /// <summary>Lua <c>nil</c>, and the value of an absent table key.</summary>
    public sealed record Nil : ScriptValue
    {
        /// <summary>The single nil instance.</summary>
        public static readonly Nil Instance = new();
    }

    /// <summary>A Lua string.</summary>
    public sealed record Str(string Value) : ScriptValue;

    /// <summary>A Lua number. Lua has no integer/float distinction in this position.</summary>
    public sealed record Num(double Value) : ScriptValue;

    /// <summary>A Lua boolean.</summary>
    public sealed record Bool(bool Value) : ScriptValue;

    /// <summary>A table with consecutive integer keys from 1.</summary>
    public sealed record List(IReadOnlyList<ScriptValue> Items) : ScriptValue;

    /// <summary>A table with string keys.</summary>
    public sealed record Map(IReadOnlyDictionary<string, ScriptValue> Entries) : ScriptValue;

    /// <summary>
    /// A function a script wrote, held so the host can call it back later. The
    /// interpreter that made it has to outlive the run that declared it, which is why
    /// the host is owned by the mod system rather than by one run.
    /// </summary>
    /// <param name="Call">Invokes it, and says what it returned.</param>
    public sealed record Func(Func<IReadOnlyList<ScriptValue>, ScriptValue> Call) : ScriptValue;

    /// <summary>Renders the Lua type name, for use in error messages.</summary>
    public string TypeName => this switch
    {
        Nil => "nil",
        Str => "string",
        Num => "number",
        Bool => "boolean",
        List => "list",
        Map => "table",
        Func => "function",
        _ => "unknown",
    };
}
