using MoonTweaks.Api;
using MoonTweaks.Scripting;
using Vintagestory.API.Server;

namespace MoonTweaks.Host;

/// <summary>The running server itself, rather than the world it is running.</summary>
/// <remarks>
/// The interpreter is sandboxed to the point of having no clock of its own: the
/// standard library it is given carries no <c>os</c>, so a script cannot time
/// anything without being told what time it is. That is what this exists for so far.
/// </remarks>
[LuaModule("moontweaks.server")]
public sealed class ServerDomain(ICoreServerAPI api, ScriptTimers timers)
{
    /// <summary>
    /// Runs a handler over and over, waiting the given milliseconds between each.
    /// Answering <c>false</c> from the handler stops it.
    /// </summary>
    /// <remarks>
    /// This is how a long job is done without the server stopping for it. Everything
    /// a script does runs on the main thread, so a handler that works for a second is
    /// a second in which the server serves nobody; the same work cut into slices, one
    /// slice per timer, costs the same total and nobody notices.
    /// </remarks>
    /// <param name="origin">Script line asking for it.</param>
    /// <param name="milliseconds">How long to wait between runs. Zero runs it every tick.</param>
    /// <param name="handler">Called each time it comes round.</param>
    [LuaFunction("every")]
    public void Every(
        ScriptOrigin origin,
        int milliseconds,
        [LuaPayload(typeof(TimerPayload), Returns = "boolean|nil")] ScriptValue.Func handler) =>
        timers.Every(milliseconds, origin, handler);

    /// <summary>Runs a handler once, the given milliseconds from now.</summary>
    /// <param name="origin">Script line asking for it.</param>
    /// <param name="milliseconds">How long to wait first.</param>
    /// <param name="handler">Called when the wait is over.</param>
    [LuaFunction("after")]
    public void After(
        ScriptOrigin origin,
        int milliseconds,
        [LuaPayload(typeof(TimerPayload))] ScriptValue.Func handler) =>
        timers.After(milliseconds, origin, handler);

    /// <summary>
    /// Milliseconds the server has been running. Real time rather than the world's,
    /// which the calendar keeps, so the difference between two of these is how long
    /// something actually took.
    /// </summary>
    /// <param name="origin">Script line asking.</param>
    [LuaFunction("elapsedMs")]
    public double ElapsedMs(ScriptOrigin origin) => api.World.ElapsedMilliseconds;
}
