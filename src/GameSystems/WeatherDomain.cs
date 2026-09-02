using MoonTweaks.Api;
using MoonTweaks.Scripting;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace MoonTweaks.GameSystems;

/// <summary>
/// The weather the world is actually having: how hard it is raining somewhere, what
/// is falling, and whether the sky has been taken over by a script.
/// </summary>
/// <remarks>
/// This is the live weather rather than the climate. <c>moontweaks.world.climateAt</c>
/// answers what a place is like in general — how wet a year is there — and these
/// answer what is happening at this moment.
///
/// Reached through the weather system the game's own <c>game</c> mod declares, which
/// is another mod's internals rather than the versioned API. A server running without
/// it is told so by name rather than answered with a guess.
/// </remarks>
/// <example>
/// <code>
/// local weather = moontweaks.weather
///
/// moontweaks.commands.add {
///   name = "storm",
///   description = "Make it pour, until somebody says otherwise",
///   privilege = "controlserver",
///   handler = function()
///     weather.setPrecipitation(1.0)
///     return "It is raining everywhere."
///   end,
/// }
///
/// -- Handing the sky back to the simulation.
/// moontweaks.commands.add {
///   name = "calm",
///   description = "Let the weather run itself again",
///   privilege = "controlserver",
///   handler = function()
///     weather.clearPrecipitation()
///     return "The weather is its own again."
///   end,
/// }
/// </code>
/// </example>
[LuaModule("moontweaks.weather")]
public sealed class WeatherDomain(GameSystems systems)
{
    /// <summary>Whether this server has the weather system at all.</summary>
    /// <remarks>
    /// Every other function here fails on a server without it, naming the mod. This is
    /// how a script meant to run on servers either way asks first.
    /// </remarks>
    /// <param name="origin">Script line asking.</param>
    [LuaFunction("available")]
    public bool Available(ScriptOrigin origin) => systems.Has<WeatherSystemServer>();

    /// <summary>
    /// How hard it is coming down at a place, from 0 for nothing to 1 for as hard as
    /// the game makes it.
    /// </summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="x">The position asked about, east to west.</param>
    /// <param name="y">The position asked about, from the world's floor upwards.</param>
    /// <param name="z">The position asked about, north to south.</param>
    [LuaFunction("precipitation")]
    public double Precipitation(ScriptOrigin origin, double x, double y, double z) =>
        Weather("weather.precipitation", origin).GetPrecipitation(x, y, z);

    /// <summary>
    /// What is falling at a place and how hard, as a table carrying <c>level</c> and
    /// <c>kind</c>.
    /// </summary>
    /// <remarks>
    /// <c>kind</c> is what the weather would drop there if it were dropping anything,
    /// so it reads <c>snow</c> on a cold mountain in clear weather. Read <c>level</c>
    /// to know whether anything is actually falling.
    /// </remarks>
    /// <param name="origin">Script line asking.</param>
    /// <param name="x">The position asked about, east to west.</param>
    /// <param name="y">The position asked about, from the world's floor upwards.</param>
    /// <param name="z">The position asked about, north to south.</param>
    [LuaFunction("falling")]
    public PrecipitationPayload Falling(ScriptOrigin origin, double x, double y, double z) =>
        new(Weather("weather.falling", origin).GetPrecipitationState(new Vec3d(x, y, z)));

    /// <summary>
    /// How wet a place has been over the days just gone, which decides whether the
    /// ground is soaked rather than whether it is raining now.
    /// </summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="x">The position asked about, east to west.</param>
    /// <param name="y">The position asked about, from the world's floor upwards.</param>
    /// <param name="z">The position asked about, north to south.</param>
    /// <param name="days">How many in-game days back to take into account.</param>
    [LuaFunction("wetness")]
    public double Wetness(ScriptOrigin origin, int x, int y, int z, double days) =>
        Weather("weather.wetness", origin)
            .GetEnvironmentWetness(new BlockPos(x, y, z), days);

    /// <summary>
    /// Holds the whole world's precipitation at one level until something clears it,
    /// from 0 for a clear sky to 1 for as hard as it comes down.
    /// </summary>
    /// <remarks>
    /// This overrides the simulation rather than nudging it: the weather goes on
    /// running underneath and nobody sees it until the override is lifted. Whatever
    /// sets one is responsible for clearing it, since nothing else will.
    /// </remarks>
    /// <param name="origin">Script line setting it.</param>
    /// <param name="level">How hard it comes down everywhere, from 0 to 1.</param>
    [LuaFunction("setPrecipitation")]
    public void SetPrecipitation(ScriptOrigin origin, double level) =>
        Weather("weather.setPrecipitation", origin).OverridePrecipitation = (float)level;

    /// <summary>Hands the weather back to the simulation, undoing <c>setPrecipitation</c>.</summary>
    /// <param name="origin">Script line clearing it.</param>
    [LuaFunction("clearPrecipitation")]
    public void ClearPrecipitation(ScriptOrigin origin) =>
        Weather("weather.clearPrecipitation", origin).OverridePrecipitation = null;

    /// <summary>
    /// The level the whole world's precipitation is being held at, or nil where the
    /// weather is running itself.
    /// </summary>
    /// <param name="origin">Script line asking.</param>
    [LuaFunction("overridden")]
    public double? Overridden(ScriptOrigin origin) =>
        Weather("weather.overridden", origin).OverridePrecipitation;

    /// <summary>
    /// Calls down a flash of lightning at a place. This is the flash and the noise
    /// rather than a strike: nothing is set alight and nobody is hurt by it.
    /// </summary>
    /// <param name="origin">Script line calling it down.</param>
    /// <param name="x">Where it comes down, east to west.</param>
    /// <param name="y">Where it comes down, from the world's floor upwards.</param>
    /// <param name="z">Where it comes down, north to south.</param>
    [LuaFunction("lightning")]
    public void Lightning(ScriptOrigin origin, double x, double y, double z) =>
        Weather("weather.lightning", origin).SpawnLightningFlash(new Vec3d(x, y, z));

    /// <summary>The weather system, or a failure naming the mod that declares it.</summary>
    private WeatherSystemServer Weather(string what, ScriptOrigin origin) =>
        systems.Required<WeatherSystemServer>("game", what, origin);
}

/// <summary>What is falling at a place, and how hard.</summary>
/// <param name="state">What the weather system said.</param>
[LuaTable("Precipitation", Given = true)]
public sealed class PrecipitationPayload(PrecipitationState state)
{
    /// <summary>How hard it is coming down, from 0 for nothing to 1 for as hard as it gets.</summary>
    [LuaField("level")]
    public double Level { get; } = state.Level;

    /// <summary>
    /// What is falling, or would be if anything were: <c>rain</c>, <c>snow</c> or
    /// <c>hail</c>.
    /// </summary>
    [LuaField("kind")]
    public EnumFallingKind Kind { get; } = ValueSet.As<EnumFallingKind>(state.Type);

    /// <summary>How big the drops or flakes are, which the game draws with.</summary>
    [LuaField("size")]
    public double Size { get; } = state.ParticleSize;
}

/// <summary>What the weather drops at a place.</summary>
public enum EnumFallingKind
{
    /// <summary>Rain.</summary>
    Rain,

    /// <summary>Snow.</summary>
    Snow,

    /// <summary>Hail.</summary>
    Hail,

    /// <summary>Whatever the temperature there calls for.</summary>
    Auto,
}
