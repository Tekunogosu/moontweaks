using System;
using System.Collections.Generic;
using MoonSharp.Interpreter;

namespace MoonTweaks.Scripting;

/// <summary>
/// MoonSharp-backed script host. Scripts run under the hard sandbox, so they can
/// reach neither the filesystem nor the CLR except through bound modules.
/// </summary>
public sealed class MoonSharpHost : IScriptHost
{
    /// <summary>Name this engine is selected by in the settings file.</summary>
    public const string EngineName = "moonsharp";

    private readonly Script script = new(CoreModules.Preset_HardSandbox);

    /// <summary>Lua version string the embedded interpreter reports.</summary>
    public string EngineVersion => script.DoString("return _VERSION").String;

    /// <inheritdoc/>
    public void Bind(ModuleBinding module)
    {
        var table = ResolvePath(module.Path);

        foreach (var function in module.Functions)
        {
            var bound = function;
            table[bound.Name] = DynValue.NewCallback((context, arguments) =>
            {
                var origin = OriginOf(context);
                var values = new ScriptValue[arguments.Count];
                for (var i = 0; i < arguments.Count; i++) values[i] = ToScriptValue(arguments[i]);
                return ToDynValue(bound.Invoke(origin, values));
            });
        }
    }

    /// <inheritdoc/>
    public void Run(ScriptFile file)
    {
        try
        {
            script.DoString(file.Source, codeFriendlyName: file.Name);
        }
        catch (ScriptRuntimeException e)
        {
            throw new ScriptError(OriginOf(e.DecoratedMessage, file.Name), e.Message);
        }
        catch (SyntaxErrorException e)
        {
            throw new ScriptError(OriginOf(e.DecoratedMessage, file.Name), e.Message);
        }
    }

    /// <inheritdoc/>
    public void Dispose() { }

    /// <summary>Walks or creates the nested tables named by a dotted path.</summary>
    private Table ResolvePath(string path)
    {
        var table = script.Globals;
        foreach (var segment in path.Split('.'))
        {
            if (table.Get(segment).Type != DataType.Table) table[segment] = new Table(script);
            table = table.Get(segment).Table;
        }
        return table;
    }

    /// <summary>Reads the calling script and line out of the interpreter's callstack.</summary>
    private ScriptOrigin OriginOf(ScriptExecutionContext context)
    {
        var location = context.CallingLocation;
        if (location is null) return ScriptOrigin.None;

        var source = script.GetSourceCode(location.SourceIdx);
        return new ScriptOrigin(source?.Name ?? "<script>", location.FromLine);
    }

    /// <summary>Recovers a line number from a decorated message when no callstack is available.</summary>
    private static ScriptOrigin OriginOf(string? decorated, string fallbackName)
    {
        // Decorated messages read "chunk:(line,col-col): text"; anything else stays line 0.
        if (decorated is not null)
        {
            var open = decorated.IndexOf(":(", StringComparison.Ordinal);
            var comma = open >= 0 ? decorated.IndexOf(',', open) : -1;
            if (comma > open + 2 && int.TryParse(decorated[(open + 2)..comma], out var line))
            {
                return new ScriptOrigin(decorated[..open], line);
            }
        }
        return new ScriptOrigin(fallbackName, 0);
    }

    /// <summary>Reduces a MoonSharp value to the neutral model.</summary>
    private ScriptValue ToScriptValue(DynValue value) => value.Type switch
    {
        DataType.String => new ScriptValue.Str(value.String),
        DataType.Number => new ScriptValue.Num(value.Number),
        DataType.Boolean => new ScriptValue.Bool(value.Boolean),
        DataType.Table => ToScriptValue(value.Table),
        DataType.Function or DataType.ClrFunction => Callable(value),
        _ => ScriptValue.Nil.Instance,
    };

    /// <summary>
    /// Wraps a script function so the host can call it after the run that declared it
    /// has finished. The interpreter is not thread safe, so callers are responsible
    /// for arriving on the thread the game runs its events on.
    /// </summary>
    private ScriptValue.Func Callable(DynValue function) =>
        new(arguments => ToScriptValue(
            script.Call(function, [.. System.Linq.Enumerable.Select(arguments, ToDynValue)])));

    /// <summary>A table with any integer key at 1 is a list; anything else is a map.</summary>
    private ScriptValue ToScriptValue(Table table)
    {
        if (table.Get(1).Type != DataType.Nil)
        {
            var items = new List<ScriptValue>(table.Length);
            for (var i = 1; i <= table.Length; i++) items.Add(ToScriptValue(table.Get(i)));
            return new ScriptValue.List(items);
        }

        var entries = new Dictionary<string, ScriptValue>();
        foreach (var pair in table.Pairs)
        {
            if (pair.Key.Type == DataType.String) entries[pair.Key.String] = ToScriptValue(pair.Value);
        }
        return new ScriptValue.Map(entries);
    }

    /// <summary>Lifts a neutral value back into the interpreter.</summary>
    private DynValue ToDynValue(ScriptValue value) => value switch
    {
        ScriptValue.Str s => DynValue.NewString(s.Value),
        ScriptValue.Num n => DynValue.NewNumber(n.Value),
        ScriptValue.Bool b => DynValue.NewBoolean(b.Value),
        // Tables travel this way only when the host calls a script rather than the
        // other way round, which is what an event handler is.
        ScriptValue.List list => ToDynValue(list),
        ScriptValue.Map map => ToDynValue(map),
        _ => DynValue.Nil,
    };

    /// <summary>A list as a Lua table with consecutive integer keys from 1.</summary>
    private DynValue ToDynValue(ScriptValue.List list)
    {
        var table = new Table(script);
        for (var i = 0; i < list.Items.Count; i++) table[i + 1] = ToDynValue(list.Items[i]);
        return DynValue.NewTable(table);
    }

    /// <summary>A map as a Lua table with string keys.</summary>
    private DynValue ToDynValue(ScriptValue.Map map)
    {
        var table = new Table(script);
        foreach (var (key, entry) in map.Entries) table[key] = ToDynValue(entry);
        return DynValue.NewTable(table);
    }
}
