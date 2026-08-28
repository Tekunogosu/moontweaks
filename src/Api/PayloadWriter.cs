using System.Collections.Generic;
using System.Reflection;
using MoonTweaks.Scripting;

namespace MoonTweaks.Api;

/// <summary>
/// Turns an object into the Lua table a script reads, using the same
/// <see cref="LuaFieldAttribute"/> metadata the reference documentation is generated
/// from. The mirror of <see cref="SpecBinder"/>, which reads a table the other way,
/// and for the same reason: what a script is handed and what the docs promise cannot
/// drift apart.
/// </summary>
public static class PayloadWriter
{
    /// <summary>Writes every documented field of one shape as the table a handler is given.</summary>
    public static ScriptValue.Map Table(object payload)
    {
        var entries = new Dictionary<string, ScriptValue>();

        foreach (var (key, property) in SpecBinder.FieldsOf(payload.GetType()))
        {
            entries[key] = Value(property.GetValue(payload));
        }

        return new ScriptValue.Map(entries);
    }

    /// <summary>
    /// Lifts one CLR value into the neutral model. Sole owner of that question, so a
    /// value handed back from a function and one read off an event table are the
    /// same value written the same way.
    /// </summary>
    public static ScriptValue Value(object? value) => value switch
    {
        null => ScriptValue.Nil.Instance,
        string s => new ScriptValue.Str(s),
        int i => new ScriptValue.Num(i),
        double d => new ScriptValue.Num(d),
        float f => new ScriptValue.Num(f),
        bool b => new ScriptValue.Bool(b),
        // A binding that has already built the shape it wants to hand over says so,
        // which is how a reading function returns a table rather than one number.
        ScriptValue built => built,
        System.Enum named => new ScriptValue.Str(named.ToString().ToLowerInvariant()),
        _ => ScriptValue.Nil.Instance,
    };
}
