using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Lua;
using Lua.Runtime;
using Lua.Standard;

namespace MoonTweaks.Scripting;

/// <summary>
/// Lua-CSharp-backed script host, and the only place an interpreter appears. Scripts
/// reach neither the filesystem nor the CLR except through bound modules.
/// </summary>
/// <remarks>
/// Lua-CSharp expresses every call as a <see cref="ValueTask{T}"/>, where
/// <see cref="IScriptHost"/> is synchronous. Nothing bound here yields: a binding is
/// CPU work against the game's own state, and the sandbox withholds the coroutine
/// library that would let a script suspend. Every such task therefore completes
/// before it is returned, and <see cref="Wait{T}"/> reads the result out of it
/// rather than blocking on anything. A binding that one day does yield would block
/// the server thread instead, which is why none may.
/// </remarks>
public sealed class LuaCSharpHost : IScriptHost
{
    /// <summary>Name this engine is selected by in the settings file.</summary>
    public const string ENGINE_NAME = "luacsharp";

    /// <summary>
    /// Globals the basic library defines that this host takes back out. Withheld for
    /// what they reach rather than for what they are called: each of these compiles
    /// or loads code the bindings never offered. A facility that merely lets a script
    /// misbehave on its own, such as <c>pcall</c>, is not on this list.
    /// </summary>
    private static readonly string[] Ungranted = ["dofile", "loadfile", "load", "loadstring"];

    private readonly LuaState state = LuaState.Create();

    /// <summary>
    /// Builds the interpreter with the libraries a script is allowed: the basic
    /// library, <c>string</c>, <c>table</c>, <c>math</c> and <c>bit32</c>. The rest
    /// are never opened, so <c>io</c>, <c>os</c>, <c>package</c>, <c>coroutine</c>,
    /// <c>debug</c> and <c>utf8</c> are absent rather than present and refusing.
    /// A script therefore has no clock of its own, which is what
    /// <c>moontweaks.server.elapsedMs</c> exists to answer.
    /// </summary>
    public LuaCSharpHost()
    {
        state.OpenBasicLibrary();
        state.OpenStringLibrary();
        state.OpenTableLibrary();
        state.OpenMathLibrary();
        state.OpenBitwiseLibrary();

        // The basic library brings these, and each is a way to reach code or files
        // the bindings never offered: the first two read the disk through the
        // platform, and the rest compile a string a script built at runtime.
        foreach (var reachable in Ungranted) state.Environment[reachable] = LuaValue.Nil;
    }

    /// <summary>Lua version string the embedded interpreter reports.</summary>
    public string EngineVersion => state.Environment["_VERSION"].ToString();

    /// <inheritdoc/>
    public void Bind(ModuleBinding module)
    {
        var table = ResolvePath(module.Path);

        foreach (var function in module.Functions)
        {
            var bound = function;
            table[bound.Name] = new LuaFunction(bound.Name, (context, _) =>
            {
                var origin = OriginOf(context);
                var values = new ScriptValue[context.ArgumentCount];
                for (var i = 0; i < values.Length; i++) values[i] = ToScriptValue(context.GetArgument(i));
                return new ValueTask<int>(context.Return(ToLuaValue(bound.Invoke(origin, values))));
            });
        }
    }

    /// <inheritdoc/>
    public void Run(ScriptFile file)
    {
        try
        {
            var chunk = state.Load(file.Source.AsSpan(), file.Name, state.Environment);
            // A chunk's own return value is nobody's, so what it left is dropped
            // rather than allowed to pile up across the scripts that follow it.
            state.Pop(Wait(state.RunAsync(chunk, CancellationToken.None)));
        }
        catch (LuaCompileException e)
        {
            throw new ScriptError(new ScriptOrigin(file.Name, e.Position.Line), e.MessageWithNearToken);
        }
        catch (LuaRuntimeException e)
        {
            // An error this mod raised inside a bound call comes back wrapped, and it
            // already names the line the binding read off the callstack. Rebuilding
            // one around its message would name that line a second time, since
            // LuaRuntimeException.Message hands back the exception it wraps.
            if (e.GetBaseException() is ScriptError raised) throw raised;

            throw new ScriptError(OriginOf(e, file.Name), MessageOf(e));
        }
    }

    /// <inheritdoc/>
    public void Dispose() => state.Dispose();

    /// <summary>Walks or creates the nested tables named by a dotted path.</summary>
    private LuaTable ResolvePath(string path)
    {
        var table = state.Environment;
        foreach (var segment in path.Split('.'))
        {
            if (table[segment].Type != LuaValueType.Table) table[segment] = new LuaTable();
            table = table[segment].Read<LuaTable>();
        }
        return table;
    }

    /// <summary>Reads the calling script and line out of the interpreter's callstack.</summary>
    /// <remarks>
    /// Read off the frames rather than from <c>GetTraceback</c>, which builds an
    /// object describing the whole stack. Every bound call needs the one line it was
    /// made from, and a script fills a shape tens of thousands of times a second;
    /// the two agree on the line, and only one of them allocates to say so.
    /// </remarks>
    private static ScriptOrigin OriginOf(LuaFunctionExecutionContext context)
    {
        var frames = context.State.GetCallStackFrames();
        if (frames.Length == 0) return ScriptOrigin.None;

        // The topmost frame is this binding, and it carries the instruction its
        // caller stopped at. The nearest Lua frame below is the script that holds it.
        var instruction = frames[^1].CallerInstructionIndex;

        for (var i = frames.Length - 1; i >= 0; i--)
        {
            if (frames[i].Function is not LuaClosure closure) continue;

            var lines = closure.Proto.LineInfo;
            return new ScriptOrigin(closure.Name,
                instruction >= 0 && instruction < lines.Length ? lines[instruction] : 0);
        }

        return ScriptOrigin.None;
    }

    /// <summary>Reads the failing script and line out of a runtime failure.</summary>
    private static ScriptOrigin OriginOf(LuaRuntimeException error, string fallbackName) =>
        error.LuaTraceback is { } traceback
            ? new ScriptOrigin(traceback.RootFunc?.Name ?? fallbackName, traceback.LastLine)
            : new ScriptOrigin(fallbackName, 0);

    /// <summary>
    /// The failure a script author should read. The exception's own message repeats
    /// the engine's name and the location this host has already put in the origin,
    /// where the error object is the sentence Lua itself raised.
    /// </summary>
    private static string MessageOf(LuaRuntimeException error) =>
        error.ErrorObject.Type == LuaValueType.Nil ? error.Message : error.ErrorObject.ToString();

    /// <summary>Reads a synchronously completed task, which is every task here.</summary>
    private static T Wait<T>(ValueTask<T> work) =>
        work.IsCompletedSuccessfully ? work.Result : work.AsTask().GetAwaiter().GetResult();

    /// <summary>Reduces a Lua-CSharp value to the neutral model.</summary>
    private ScriptValue ToScriptValue(LuaValue value) => value.Type switch
    {
        LuaValueType.String => new ScriptValue.Str(value.Read<string>()),
        LuaValueType.Number => new ScriptValue.Num(value.Read<double>()),
        LuaValueType.Boolean => new ScriptValue.Bool(value.Read<bool>()),
        LuaValueType.Table => ToScriptValue(value.Read<LuaTable>()),
        LuaValueType.Function => Callable(value),
        _ => ScriptValue.Nil.Instance,
    };

    /// <summary>
    /// Wraps a script function so the host can call it after the run that declared it
    /// has finished. The interpreter is not thread safe, so callers are responsible
    /// for arriving on the thread the game runs its events on.
    /// </summary>
    private ScriptValue.Func Callable(LuaValue function) =>
        new(arguments =>
        {
            var supplied = new LuaValue[arguments.Count];
            for (var i = 0; i < supplied.Length; i++) supplied[i] = ToLuaValue(arguments[i]);

            var results = Wait(state.CallAsync(function, supplied, CancellationToken.None));
            return results.Length > 0 ? ToScriptValue(results[0]) : ScriptValue.Nil.Instance;
        });

    /// <summary>
    /// A table with anything in its array part is a list; anything else is a map.
    /// The rule belongs to the neutral model rather than to this engine, so a shape a
    /// script writes reads back the same whichever interpreter is running it.
    /// </summary>
    private ScriptValue ToScriptValue(LuaTable table)
    {
        if (table.ArrayLength > 0)
        {
            // Read off the array part directly. Going through the indexer would send
            // every element round the key lookup a map needs and a list does not.
            var array = table.GetArraySpan()[..table.ArrayLength];
            var items = new List<ScriptValue>(array.Length);
            foreach (var item in array) items.Add(ToScriptValue(item));
            return new ScriptValue.List(items);
        }

        var entries = new Dictionary<string, ScriptValue>();
        foreach (var pair in table)
        {
            if (pair.Key.Type == LuaValueType.String) entries[pair.Key.Read<string>()] = ToScriptValue(pair.Value);
        }
        return new ScriptValue.Map(entries);
    }

    /// <summary>Lifts a neutral value back into the interpreter.</summary>
    private LuaValue ToLuaValue(ScriptValue value) => value switch
    {
        ScriptValue.Str s => s.Value,
        ScriptValue.Num n => n.Value,
        ScriptValue.Bool b => b.Value,
        // Tables travel this way only when the host calls a script rather than the
        // other way round, which is what an event handler is.
        ScriptValue.List list => ToLuaValue(list),
        ScriptValue.Map map => ToLuaValue(map),
        _ => LuaValue.Nil,
    };

    /// <summary>A list as a Lua table with consecutive integer keys from 1.</summary>
    private LuaValue ToLuaValue(ScriptValue.List list)
    {
        var table = new LuaTable(list.Items.Count, 0);
        for (var i = 0; i < list.Items.Count; i++) table[i + 1] = ToLuaValue(list.Items[i]);
        return table;
    }

    /// <summary>A map as a Lua table with string keys.</summary>
    private LuaValue ToLuaValue(ScriptValue.Map map)
    {
        var table = new LuaTable(0, map.Entries.Count);
        foreach (var (key, entry) in map.Entries) table[key] = ToLuaValue(entry);
        return table;
    }
}
