using MoonTweaks.Api;
using Vintagestory.API.Common;

namespace MoonTweaks.World;

// What a script is told when it asks the world what time it is.

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

    /// <summary>How much of the moon is lit, which decides how dark a night is.</summary>
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
