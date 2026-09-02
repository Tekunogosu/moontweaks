using MoonTweaks.Api;
using MoonTweaks.Scripting;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace MoonTweaks.GameSystems;

/// <summary>
/// Temporal stability: how sound a place is, and when the next storm is due.
/// </summary>
/// <remarks>
/// Stability is a property of a place rather than of a player — deep and far from the
/// surface is worse — and it decides what spawns there and what the world does to
/// somebody standing in it. A world with temporal stability turned off answers 2
/// everywhere, which is above the 1 an untroubled place reads, so a script can tell
/// the two apart.
///
/// Reached through the survival mod's own system rather than the versioned API, so a
/// server running without that mod is told so by name.
/// </remarks>
/// <example>
/// <code>
/// local stability = moontweaks.stability
///
/// moontweaks.events.playerReady(function(e)
///   local at = moontweaks.players.position(e.player)
///   local sound = stability.at(at.x, at.y, at.z)
///
///   if sound &lt; 0.5 then
///     moontweaks.players.warn(e.player, "The air here does not feel right.")
///   end
/// end)
///
/// -- Telling everybody a storm is coming, once a minute.
/// moontweaks.server.every(60000, function()
///   local storm = stability.storm()
///   if storm.active then
///     moontweaks.log.info(("a %s storm is running"):format(storm.strength))
///   end
/// end)
/// </code>
/// </example>
[LuaModule("moontweaks.stability")]
public sealed class StabilityDomain(GameSystems systems)
{
    /// <summary>Whether this server has the temporal stability system at all.</summary>
    /// <param name="origin">Script line asking.</param>
    [LuaFunction("available")]
    public bool Available(ScriptOrigin origin) => systems.Has<SystemTemporalStability>();

    /// <summary>
    /// How stable a place is. Around 1 where the world is sound and towards 0 where it
    /// is not; 2 everywhere on a world with temporal stability turned off.
    /// </summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="x">The position asked about, east to west.</param>
    /// <param name="y">The position asked about, from the world's floor upwards.</param>
    /// <param name="z">The position asked about, north to south.</param>
    [LuaFunction("at")]
    public double At(ScriptOrigin origin, double x, double y, double z) =>
        Stability("stability.at", origin).GetTemporalStability(x, y, z);

    /// <summary>
    /// The temporal storm: whether one is running, how strong it is, and when the next
    /// one is due.
    /// </summary>
    /// <param name="origin">Script line asking.</param>
    [LuaFunction("storm")]
    public StormPayload Storm(ScriptOrigin origin) =>
        new(Stability("stability.storm", origin).StormData);

    /// <summary>The stability system, or a failure naming the mod that declares it.</summary>
    private SystemTemporalStability Stability(string what, ScriptOrigin origin) =>
        systems.Required<SystemTemporalStability>("survival", what, origin);
}

/// <summary>What the temporal storms are doing.</summary>
/// <param name="data">What the stability system said.</param>
[LuaTable("Storm", Given = true)]
public sealed class StormPayload(TemporalStormRunTimeData data)
{
    /// <summary>Whether a storm is running right now.</summary>
    [LuaField("active")]
    public bool Active { get; } = data.nowStormActive;

    /// <summary>
    /// How hard the world is being pulled apart, from 0 between storms upwards. This is
    /// what actually reaches players, where <c>strength</c> is the name of the storm
    /// that is coming.
    /// </summary>
    [LuaField("glitch")]
    public double Glitch { get; } = data.stormGlitchStrength;

    /// <summary>How strong the next storm will be.</summary>
    [LuaField("strength")]
    public EnumStormKind Strength { get; } = ValueSet.As<EnumStormKind>(data.nextStormStrength);

    /// <summary>
    /// The day the next storm begins, counted from the world's first, so subtract
    /// <c>moontweaks.calendar.now().totalDays</c> to know how long there is.
    /// </summary>
    [LuaField("nextDay")]
    public double NextDay { get; } = data.nextStormTotalDays;

    /// <summary>The day the running storm began, which is meaningless while none is.</summary>
    [LuaField("sinceDay")]
    public double SinceDay { get; } = data.stormActiveTotalDays;
}

/// <summary>How hard a temporal storm pulls.</summary>
public enum EnumStormKind
{
    /// <summary>Light.</summary>
    Light,

    /// <summary>Medium.</summary>
    Medium,

    /// <summary>Heavy.</summary>
    Heavy,
}
