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

        // Written to disk by the server on its own schedule rather than here, so a
        // handler changing a rule every tick costs one write rather than one a tick.
        api.Server.MarkConfigDirty();
    }
}

/// <summary>Facts about the running server that do not change while it runs.</summary>
/// <param name="api">The running server.</param>
[LuaTable("ServerInfo", Given = true)]
public sealed class ServerPayload(ICoreServerAPI api)
{
    /// <summary>What the server calls itself in the browser.</summary>
    [LuaField("name")]
    public string Name { get; } = api.Server.Config.ServerName ?? "";

    /// <summary>The message a player is shown as they arrive.</summary>
    [LuaField("welcome")]
    public string Welcome { get; } = api.Server.Config.WelcomeMessage ?? "";

    /// <summary>How many players may be here at once.</summary>
    [LuaField("maxPlayers")]
    public int MaxPlayers { get; } = api.Server.Config.MaxClients;

    /// <summary>How many are here now.</summary>
    [LuaField("players")]
    public int Players { get; } = api.World.AllOnlinePlayers.Length;

    /// <summary>Real milliseconds since the server started.</summary>
    [LuaField("uptimeMs")]
    public double UptimeMs { get; } = api.Server.ServerUptimeMilliseconds;

    /// <summary>Real seconds anybody has spent playing here, added up across every player.</summary>
    [LuaField("totalPlayTime")]
    public double TotalPlayTime { get; } = api.Server.TotalWorldPlayTime;

    /// <summary>What the world is called.</summary>
    [LuaField("worldName")]
    public string WorldName { get; } = api.World.SavegameIdentifier ?? "";

    /// <summary>The number every part of this world was generated from.</summary>
    [LuaField("seed")]
    public int Seed { get; } = api.World.Seed;

    /// <summary>The height the sea sits at, which is what "above ground" is measured from.</summary>
    [LuaField("seaLevel")]
    public int SeaLevel { get; } = api.World.SeaLevel;

    /// <summary>How far the world runs, east to west.</summary>
    [LuaField("mapSizeX")]
    public int MapSizeX { get; } = api.WorldManager.MapSizeX;

    /// <summary>How far the world runs, from its floor to its ceiling.</summary>
    [LuaField("mapSizeY")]
    public int MapSizeY { get; } = api.WorldManager.MapSizeY;

    /// <summary>How far the world runs, north to south.</summary>
    [LuaField("mapSizeZ")]
    public int MapSizeZ { get; } = api.WorldManager.MapSizeZ;
}

/// <summary>The rules a server is running under, each of which it may change.</summary>
/// <remarks>
/// Every key is optional and only the ones a script writes change, so a script may
/// turn combat off without restating everything else the server allows.
/// </remarks>
[LuaTable("ServerRules")]
public sealed class RulesSpec
{
    /// <summary>Whether players may hurt each other.</summary>
    [LuaField("pvp")]
    public bool? Pvp { get; set; }

    /// <summary>Whether fire spreads from what it is burning to what is beside it.</summary>
    [LuaField("fireSpread")]
    public bool? FireSpread { get; set; }

    /// <summary>Whether sand and gravel fall when what held them up is taken away.</summary>
    [LuaField("fallingBlocks")]
    public bool? FallingBlocks { get; set; }
}
