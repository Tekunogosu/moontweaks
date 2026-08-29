using MoonTweaks.Api;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace MoonTweaks.World;

/// <summary>What the world's clock reads.</summary>
/// <param name="calendar">The calendar the game keeps.</param>
[LuaTable("CalendarReading", Given = true)]
public sealed class CalendarPayload(IGameCalendar calendar)
{
    /// <summary>
    /// How far through the day it is, from 0 at midnight. Fractional, so half past
    /// nine in the morning reads 9.5.
    /// </summary>
    [LuaField("hourOfDay")]
    public double HourOfDay { get; } = calendar.HourOfDay;

    /// <summary>Which day of the year it is, counting from 1.</summary>
    [LuaField("dayOfYear")]
    public int DayOfYear { get; } = calendar.DayOfYear;

    /// <summary>Which year it is.</summary>
    [LuaField("year")]
    public int Year { get; } = calendar.Year;

    /// <summary>Which month it is.</summary>
    [LuaField("month")]
    public EnumMonthName Month { get; } = ValueSet.As<EnumMonthName>(calendar.MonthName);

    /// <summary>How much of the moon is lit, which is what decides how dark a night is.</summary>
    [LuaField("moonPhase")]
    public EnumMoonKind MoonPhase { get; } = ValueSet.As<EnumMoonKind>(calendar.MoonPhase);

    /// <summary>
    /// How much light the moon gives tonight, roughly 0 at new moon and 1 at full.
    /// The game's own figure dips a little below zero either side of a new moon, so
    /// clamp it before showing it to anybody as a percentage.
    /// </summary>
    [LuaField("moonBrightness")]
    public double MoonBrightness { get; } = calendar.MoonPhaseBrightness;

    /// <summary>
    /// Hours since the world began. This is the one to remember and subtract from
    /// later: unlike <c>hourOfDay</c> it never goes backwards.
    /// </summary>
    [LuaField("totalHours")]
    public double TotalHours { get; } = calendar.TotalHours;

    /// <summary>Days since the world began.</summary>
    [LuaField("totalDays")]
    public double TotalDays { get; } = calendar.TotalDays;

    /// <summary>How many hours this world puts in a day, which a server may retune.</summary>
    [LuaField("hoursPerDay")]
    public double HoursPerDay { get; } = calendar.HoursPerDay;

    /// <summary>How many days this world puts in a year.</summary>
    [LuaField("daysPerYear")]
    public int DaysPerYear { get; } = calendar.DaysPerYear;

    /// <summary>The date as the game itself writes it, for putting straight into a message.</summary>
    [LuaField("pretty")]
    public string Pretty { get; } = calendar.PrettyDate();
}

/// <summary>What season it is somewhere, which depends on where that somewhere is.</summary>
/// <param name="season">Which season the place is in.</param>
/// <param name="progress">How far through that season it is.</param>
/// <param name="hemisphere">Which half of the world the place is in.</param>
[LuaTable("SeasonReading", Given = true)]
public sealed class SeasonPayload(EnumSeasonKind season, double progress, EnumHemisphereKind hemisphere)
{
    /// <summary>Which of the four seasons it is here.</summary>
    [LuaField("season")]
    public EnumSeasonKind Season { get; } = season;

    /// <summary>
    /// How far round the year it is, from 0 at the start of spring to 1 at the end of
    /// winter. Read this rather than <c>season</c> for anything that should change
    /// gradually rather than in four steps.
    /// </summary>
    [LuaField("progress")]
    public double Progress { get; } = progress;

    /// <summary>
    /// Which half of the world this is. The southern half runs half a year out of
    /// step, so the same date is a different season depending on where it is asked.
    /// </summary>
    [LuaField("hemisphere")]
    public EnumHemisphereKind Hemisphere { get; } = hemisphere;
}

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
        world.Calendar.SetTimeSpeedModifier(Scoped(name), (float)speed);

    /// <summary>Takes back a speed change made under a name, leaving any others alone.</summary>
    /// <param name="origin">Script line taking it back.</param>
    /// <param name="name">Name it was set under.</param>
    [LuaFunction("clearSpeed")]
    public void ClearSpeed(ScriptOrigin origin, string name) =>
        world.Calendar.RemoveTimeSpeedModifier(Scoped(name));

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

    /// <summary>
    /// Speed changes are held under this mod's own prefix, so a script cannot take
    /// back one the game or another mod is holding.
    /// </summary>
    private static string Scoped(string name) => $"moontweaks:{name}";
}
