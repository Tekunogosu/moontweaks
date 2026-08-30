using MoonTweaks.Api;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace MoonTweaks.World;

/// <summary>
/// The world's own clock and calendar: what time it is, and how fast it passes.
/// </summary>
/// <remarks>
/// Distinct from <c>moontweaks.server.elapsedMs</c>, which measures real time. An
/// in-game hour is a few real minutes by default and a server may change that, so
/// anything about seasons, daylight or spoilage belongs here and anything about how
/// long something actually took belongs there.
///
/// Reading is safe wherever a script runs. Everything that writes acts on a loaded
/// world and so belongs in a handler, as the rest of the world does.
/// </remarks>
[LuaModule("moontweaks.calendar")]
public sealed class CalendarDomain(IWorldAccessor world)
{
    /// <summary>
    /// What the clock reads now, as one table. Read together rather than one call per
    /// field, because every one of them is answered from the same tick.
    /// </summary>
    /// <param name="origin">Script line asking.</param>
    [LuaFunction("now")]
    public CalendarPayload Now(ScriptOrigin origin) => new(world.Calendar);

    /// <summary>
    /// What season it is at a place. Position matters: the two halves of the world
    /// are half a year apart, so there is no answer to give without one.
    /// </summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="x">Where to ask.</param>
    /// <param name="y">Where to ask.</param>
    /// <param name="z">Where to ask.</param>
    [LuaFunction("seasonAt")]
    public SeasonPayload SeasonAt(ScriptOrigin origin, int x, int y, int z)
    {
        var at = new BlockPos(x, y, z);
        return new SeasonPayload(
            ValueSet.As<EnumSeasonKind>(world.Calendar.GetSeason(at)),
            world.Calendar.GetSeasonRel(at),
            ValueSet.As<EnumHemisphereKind>(world.Calendar.GetHemisphere(at)));
    }

    /// <summary>
    /// How bright daylight is at a place, from 0 in the dark to 1 at noon. This is
    /// the sky's own strength rather than what reaches the ground, so a block deep
    /// underground reads the same as one on the surface above it.
    /// </summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="x">Where to ask.</param>
    /// <param name="z">Where to ask.</param>
    [LuaFunction("daylightAt")]
    public double DaylightAt(ScriptOrigin origin, double x, double z) =>
        world.Calendar.GetDayLightStrength(x, z);

    /// <summary>
    /// Moves the clock forward by a number of in-game hours. Negative winds it back.
    /// </summary>
    /// <remarks>
    /// This moves time itself rather than skipping to an hour, so everything the
    /// world ages by the clock ages with it: crops grow, food spoils and the season
    /// advances by exactly what was added.
    /// </remarks>
    /// <param name="origin">Script line moving it.</param>
    /// <param name="hours">In-game hours to move forward.</param>
    [LuaFunction("add")]
    public void Add(ScriptOrigin origin, double hours) => world.Calendar.Add((float)hours);

    /// <summary>
    /// Changes how fast time passes, under a name of the script's choosing.
    /// </summary>
    /// <remarks>
    /// Named rather than set outright so that two scripts changing the speed do not
    /// silently undo each other: each holds its own, the game combines them, and
    /// <c>clearSpeed</c> takes back exactly the one it was given. Use the same name
    /// every time from the same script.
    /// </remarks>
    /// <param name="origin">Script line changing it.</param>
    /// <param name="name">Name to hold this change under, for taking it back later.</param>
    /// <param name="speed">Multiplier. 1 is ordinary, 2 is twice as fast, 0 stops the clock.</param>
    [LuaFunction("setSpeed")]
    public void SetSpeed(ScriptOrigin origin, string name, double speed) =>
        world.Calendar.SetTimeSpeedModifier(ModKey.For(name), (float)speed);

    /// <summary>Takes back a speed change made under a name, leaving any others alone.</summary>
    /// <param name="origin">Script line taking it back.</param>
    /// <param name="name">Name it was set under.</param>
    [LuaFunction("clearSpeed")]
    public void ClearSpeed(ScriptOrigin origin, string name) =>
        world.Calendar.RemoveTimeSpeedModifier(ModKey.For(name));

    /// <summary>
    /// Holds the whole world at one point in the year, whatever the date says.
    /// </summary>
    /// <param name="origin">Script line holding it.</param>
    /// <param name="progress">
    /// How far round the year to hold it, from 0 at the start of spring to 1 at the
    /// end of winter. Summer sits around 0.25 and winter around 0.75.
    /// </param>
    [LuaFunction("setSeason")]
    public void SetSeason(ScriptOrigin origin, double progress) =>
        world.Calendar.SetSeasonOverride((float)progress);

    /// <summary>Lets the seasons run with the calendar again.</summary>
    /// <param name="origin">Script line releasing it.</param>
    [LuaFunction("clearSeason")]
    public void ClearSeason(ScriptOrigin origin) => world.Calendar.SetSeasonOverride(null);

}
