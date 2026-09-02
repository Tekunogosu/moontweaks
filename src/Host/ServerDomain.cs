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
/// <example>
/// <code>
/// local server = moontweaks.server
/// local info = server.info()
///
/// moontweaks.log.info(("%s: %d of %d players, world %d blocks across")
///   :format(info.name, info.players, info.maxPlayers, info.mapSizeX))
///
/// server.setRules { pvp = false }
///
/// -- A slice of work per tick, stopping itself by answering false.
/// local ticks = 0
/// server.every(0, function()
///   ticks = ticks + 1
///   if ticks == 20 then return false end
/// end)
/// </code>
/// </example>
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

    /// <summary>
    /// What this server is and how much of it there is: its name, how many are here,
    /// how long it has been up, and how far the world runs.
    /// </summary>
    /// <param name="origin">Script line asking.</param>
    [LuaFunction("info")]
    public ServerPayload Info(ScriptOrigin origin) => new(api);

    /// <summary>
    /// The rules this server is running under. Read alongside <c>setRules</c>, which
    /// changes them.
    /// </summary>
    /// <param name="origin">Script line asking.</param>
    [LuaFunction("rules")]
    public RulesSpec Rules(ScriptOrigin origin) => new()
    {
        Pvp = api.Server.Config.AllowPvP,
        FireSpread = api.Server.Config.AllowFireSpread,
        FallingBlocks = api.Server.Config.AllowFallingBlocks,
        EntitySpawning = api.WorldManager.SaveGame.EntitySpawning,
        BlockTickInterval = api.Server.Config.BlockTickInterval,
        RandomBlockTicksPerChunk = api.Server.Config.RandomBlockTicksPerChunk,
        SpawnCapPlayerScaling = api.Server.Config.SpawnCapPlayerScaling,
    };

    /// <summary>
    /// Changes the rules this server runs under. Only the keys a script writes change.
    /// </summary>
    /// <remarks>
    /// These are settings rather than world state, so a change takes effect at once
    /// and is written back to the server's own configuration to survive a restart.
    /// A script that means a change to be temporary has to put it back itself.
    /// </remarks>
    /// <param name="origin">Script line changing them.</param>
    /// <param name="rules">Which rules to change, and to what.</param>
    [LuaFunction("setRules")]
    public void SetRules(ScriptOrigin origin, RulesSpec rules)
    {
        if (rules.Pvp is { } pvp) api.Server.Config.AllowPvP = pvp;
        if (rules.FireSpread is { } fire) api.Server.Config.AllowFireSpread = fire;
        if (rules.FallingBlocks is { } falling) api.Server.Config.AllowFallingBlocks = falling;
        if (rules.BlockTickInterval is { } interval) api.Server.Config.BlockTickInterval = interval;
        if (rules.RandomBlockTicksPerChunk is { } ticks) api.Server.Config.RandomBlockTicksPerChunk = ticks;
        if (rules.SpawnCapPlayerScaling is { } scaling)
        {
            api.Server.Config.SpawnCapPlayerScaling = (float)scaling;
        }

        // Kept with the world rather than with the server, so it is saved by the world
        // being saved and is not what MarkConfigDirty below is about.
        if (rules.EntitySpawning is { } spawning) api.WorldManager.SaveGame.EntitySpawning = spawning;

        // Written to disk by the server on its own schedule rather than here, so a
        // handler changing a rule every tick costs one write rather than one a tick.
        api.Server.MarkConfigDirty();
    }

    /// <summary>
    /// Declares a privilege, so that a command may require it and
    /// <c>moontweaks.players.hasPrivilege</c> may ask after it. A name the server has
    /// never been told about is one nobody holds, which makes a command requiring it
    /// a command nobody can run.
    /// </summary>
    /// <remarks>
    /// Declaring a privilege is not granting it. Administrators and the server
    /// console are given it on declaration, so whoever owns the server can run a
    /// script's own command the moment it is declared; everybody else
    /// gets it by the operator naming it in a role in <c>serverconfig.json</c>. What
    /// grants a privilege to a player outright stays unbound deliberately, since a
    /// script that can grant one can grant itself anything.
    ///
    /// The declaration lasts as long as the server runs and no longer, so this belongs
    /// in a script's body — which runs at every startup — rather than in a handler.
    /// </remarks>
    /// <param name="origin">Script line declaring it.</param>
    /// <param name="code">Name the privilege is required and read by.</param>
    /// <param name="description">
    /// What holding it allows, as an operator reading the role it is named in will
    /// see it.
    /// </param>
    [LuaFunction("addPrivilege")]
    public void AddPrivilege(ScriptOrigin origin, string code, string description) =>
        api.Permissions.RegisterPrivilege(code, description);
}
