using System;
using System.Collections.Generic;
using MoonTweaks.Scripting;

namespace MoonTweaks.LuaBench;

/// <summary>One timed case: a chunk to run, and how many times its body goes round.</summary>
/// <param name="Name">How the case is named in the report.</param>
/// <param name="Unit">What one operation is, so a figure says what it is a figure for.</param>
/// <param name="Iterations">How many times the body runs inside one chunk.</param>
/// <param name="Source">Lua source, with <c>$N</c> standing for the iteration count.</param>
public sealed record Case(string Name, string Unit, int Iterations, string Source)
{
    /// <summary>The chunk to run, with the iteration count written into it.</summary>
    public ScriptFile FileFor(int iterations) =>
        new($"bench:{Name}", Source.Replace("$N", iterations.ToString()));
}

/// <summary>One parity check: a chunk whose recorded value must be the same everywhere.</summary>
/// <param name="Name">How the check is named in the report.</param>
/// <param name="Source">Lua source, which must call <c>bench.record</c> exactly once.</param>
/// <param name="Known">
/// Why two engines are already understood to differ here, or null where they must
/// agree. Nothing carries one: the reasons the last swap needed went with the engine
/// they described, and a candidate's own differences are found when it is registered
/// rather than guessed at now. A difference with a reason beside it is reported and
/// does not fail the run; one without is what this is looking for, and a reason that
/// no longer applies is reported too, because a note nothing holds up is worse than
/// no note.
/// </param>
public sealed record Check(string Name, string Source, string? Known = null)
{
    /// <summary>The chunk to run.</summary>
    public ScriptFile File => new($"check:{Name}", Source);
}

/// <summary>
/// What every engine is asked to do. The cases are the shapes MoonTweaks actually
/// puts through an interpreter — a call into a binding, a spec table filled and
/// handed over, a handler called back with an event payload — rather than the
/// arithmetic a general Lua benchmark would measure.
/// </summary>
/// <remarks>
/// Bindings here are built as <see cref="ModuleBinding"/> by hand rather than through
/// <see cref="Api.DomainBinder"/>. What the binder costs is the same whichever engine
/// is underneath it, so including it would add a constant to every column and change
/// no comparison, while making the figures answer a vaguer question. That layer is
/// not left unmeasured for it: <see cref="Binder"/> measures it on its own, once,
/// which is where its cost is read rather than in any column here.
/// </remarks>
public static class Workload
{
    /// <summary>The block code a script passes, of the length real ones run to.</summary>
    private const string CODE = "game:soil-medium-none";

    /// <summary>Cases in the report's order, cheapest shape first.</summary>
    public static IReadOnlyList<Case> Cases { get; } =
    [
        new("lua: empty loop", "iteration", 2_000_000,
            "for i = 1, $N do end"),

        new("lua: arithmetic", "iteration", 2_000_000,
            "local s = 0 for i = 1, $N do s = s + i * 2 end"),

        new("lua: table write (growing)", "write", 1_000_000,
            "local t = {} for i = 1, $N do t[i] = i end"),

        new("lua: table constructor, 4 keys", "table", 1_000_000,
            $"for i = 1, $N do local t = {{ x = i, y = i, z = i, block = '{CODE}' }} end"),

        new("lua: string concat", "concat", 500_000,
            "for i = 1, $N do local s = 'game:' .. i end"),

        new("call: 4 scalar args", "call", 1_000_000,
            $"for i = 1, $N do bench.noop(i, i, i, '{CODE}') end"),

        new("call: returning a number", "call", 1_000_000,
            "for i = 1, $N do local n = bench.sum(i, i, i) end"),

        new("call: one 4-key table arg", "call", 500_000,
            $"for i = 1, $N do bench.noop({{ x = i, y = i, z = i, block = '{CODE}' }}) end"),

        new("call: one 64-entry list arg", "call", 20_000,
            "local t = {} for i = 1, 64 do t[i] = i end\n"
            + "for i = 1, $N do bench.noop(t) end"),
    ];

    /// <summary>
    /// Checks in the report's order. Each records one value: what the engine running
    /// them makes of a corner of Lua that a script could reach. With one engine that
    /// is a description of it; with two it is the test, because engines that disagree
    /// about what a script means cannot be swapped, whatever the timings say.
    /// </summary>
    public static IReadOnlyList<Check> Checks { get; } =
    [
        new("_VERSION", "bench.record(_VERSION)",
            Known: "each engine names itself rather than the language level it implements"),
        // Where a failure says it happened is as much a promise to a script author
        // as what a function returns, and it is the engine that answers for it.
        new("origin: line of the call", "\n\n\nbench.record(bench.where())"),
        new("origin: line inside a function",
            "local function deep()\n  local at = bench.where()\n  return at\nend\nbench.record(deep())"),
        // A tail call replaces its caller's frame, so the line a binding is told is
        // the one the chain started at rather than the one it was written on. Real
        // Lua does this; an engine that does not will report the inner line instead.
        new("tail call: origin through one",
            "local function deep()\n  return bench.where()\nend\nbench.record(deep())"),
        // Whether tail calls actually reuse the frame, rather than only looking as
        // though they do. An engine that grows the stack here fails on a script the
        // other runs, which is a difference no timing can make up for.
        new("tail call: deep recursion",
            "local function count(n)\n  if n == 0 then return 'reached' end\n  return count(n - 1)\nend\n"
            + "local ok, answer = pcall(count, 100000)\n"
            + "bench.record(ok and answer or 'overflowed')"),
        new("integer division", "bench.record(7 // 2)"),
        new("modulo of a negative", "bench.record(-7 % 3)"),
        new("float division", "bench.record(1 / 3)"),
        new("exponent", "bench.record(2 ^ 10)"),
        new("tostring of a whole number", "bench.record(tostring(3.0))"),
        new("tostring of a fraction", "bench.record(tostring(0.1))"),
        new("string.format %d", "bench.record(string.format('%d', 42))"),
        new("string.format %.2f", "bench.record(string.format('%.2f', 1 / 3))"),
        new("length of an ascii string", "bench.record(#'game:soil')"),
        // Lua counts bytes and Lua-CSharp counts UTF-16 units, so a code point
        // outside ASCII is where the two stop agreeing. Nothing in the asset
        // codes reaches here; a script's own strings can.
        new("length of a non-ascii string", "bench.record(#'åäö')"),
        new("string.sub", "bench.record(('game:soil'):sub(6))"),
        new("string.find pattern", "bench.record(('game:soil-medium'):find('%-') or -1)"),
        new("gsub", "bench.record(('a.b.c'):gsub('%.', '/'))"),
        new("table.concat", "bench.record(table.concat({ 'a', 'b', 'c' }, ','))"),
        new("table.sort", "local t = { 3, 1, 2 } table.sort(t) bench.record(t)"),
        new("list round trip", "bench.record({ 1, 2, 3 })"),
        new("map round trip", "bench.record({ a = 1, b = 'two' })"),
        new("nested shape", "bench.record({ name = 'x', items = { 1, 2 } })"),
        new("empty table", "bench.record({})"),
        new("boolean and nil", "bench.record({ t = true, f = false })"),
        new("math.floor", "bench.record(math.floor(7.9))"),
        new("math.huge", "bench.record(tostring(math.huge))"),
        // Whether the two mean the same number, which the rendering above cannot say.
        // Doubling an infinity leaves it; doubling the largest finite double does not.
        new("math.huge is infinite", "bench.record(math.huge > 1e308 and math.huge * 2 == math.huge)"),
        new("pcall of a failure", "local ok = pcall(function() error('x') end) bench.record(ok)"),
        new("varargs count", "local function f(...) return select('#', ...) end bench.record(f(1, nil, 3))"),
        new("sandbox: io withheld", "bench.record(io == nil)"),
        new("sandbox: os withheld", "bench.record(os == nil)"),
        new("sandbox: package withheld", "bench.record(package == nil)"),
        // An engine that leaves the table standing is asked what is in it: a
        // sandbox is what a script can reach, not what a preset is named.
        new("sandbox: what package holds",
            "if type(package) ~= 'table' then bench.record('absent') else\n"
            + "  local names = {}\n"
            + "  for k, v in pairs(package) do names[#names + 1] = tostring(k) .. ':' .. type(v) end\n"
            + "  table.sort(names)\n"
            + "  bench.record(table.concat(names, ','))\n"
            + "end"),
        new("sandbox: load withheld", "bench.record(load == nil)"),
        new("sandbox: dofile withheld", "bench.record(dofile == nil)"),
        new("sandbox: loadfile withheld", "bench.record(loadfile == nil)"),
    ];

    /// <summary>
    /// The module every case and check is run against. <c>record</c> keeps whatever a
    /// check handed over, and <c>take</c> keeps a handler so the host can call back
    /// into the script, which is the direction an event travels.
    /// </summary>
    public static ModuleBinding ModuleFor(Recorder recorder) =>
        new("bench", [
            new FunctionBinding("noop", (_, _) => ScriptValue.Nil.Instance),
            new FunctionBinding("sum", (_, values) => new ScriptValue.Num(
                Number(values, 0) + Number(values, 1) + Number(values, 2))),
            new FunctionBinding("record", (_, values) =>
            {
                recorder.Recorded = values.Count > 0 ? values[0] : ScriptValue.Nil.Instance;
                return ScriptValue.Nil.Instance;
            }),
            new FunctionBinding("where", (origin, _) => new ScriptValue.Str(origin.ToString())),
            new FunctionBinding("take", (_, values) =>
            {
                recorder.Handler = values.Count > 0 ? values[0] as ScriptValue.Func : null;
                return ScriptValue.Nil.Instance;
            }),
        ]);

    /// <summary>The chunk that leaves a handler behind for the host to call.</summary>
    public static ScriptFile HandlerFile { get; } = new("bench:handler",
        "bench.take(function(e) return e.x + e.y end)");

    /// <summary>The payload a handler is called with, of the shape a block event has.</summary>
    public static ScriptValue.Map HandlerPayload { get; } = new(new Dictionary<string, ScriptValue>
    {
        ["x"] = new ScriptValue.Num(1),
        ["y"] = new ScriptValue.Num(2),
        ["player"] = new ScriptValue.Str("Theysa"),
        ["block"] = new ScriptValue.Str(CODE),
    });

    /// <summary>One argument as a number, or zero where a script passed something else.</summary>
    private static double Number(IReadOnlyList<ScriptValue> values, int index) =>
        index < values.Count && values[index] is ScriptValue.Num number ? number.Value : 0;
}

/// <summary>What a chunk left behind for the runner to read.</summary>
public sealed class Recorder
{
    /// <summary>The value the last check recorded.</summary>
    public ScriptValue Recorded { get; set; } = ScriptValue.Nil.Instance;

    /// <summary>The handler the last run left for the host to call.</summary>
    public ScriptValue.Func? Handler { get; set; }
}
