using MoonTweaks.Api;
using Vintagestory.API.Server;

namespace MoonTweaks.Host;

// What a script is told about the running server, and what it may change about it.

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
