namespace MoonTweaks.Api;

// The closed sets a script writes as strings. Declared here rather than taken from
// the game, so this layer names no game type; the layer that applies them matches
// these to the game's own by name. These are the ones a place and
// a moment are described with: where in the year it is, and what light reaches
// somewhere.

/// <summary>One of the four seasons a place is in.</summary>
public enum EnumSeasonKind
{
    /// <summary>Spring.</summary>
    Spring,

    /// <summary>Summer.</summary>
    Summer,

    /// <summary>Autumn.</summary>
    Fall,

    /// <summary>Winter.</summary>
    Winter,
}

/// <summary>Which month of the world's year it is.</summary>
public enum EnumMonthName
{
    /// <summary>January.</summary>
    January,

    /// <summary>February.</summary>
    February,

    /// <summary>March.</summary>
    March,

    /// <summary>April.</summary>
    April,

    /// <summary>May.</summary>
    May,

    /// <summary>June.</summary>
    June,

    /// <summary>July.</summary>
    July,

    /// <summary>August.</summary>
    August,

    /// <summary>September.</summary>
    September,

    /// <summary>October.</summary>
    October,

    /// <summary>November.</summary>
    November,

    /// <summary>December.</summary>
    December,
}

/// <summary>How much of the moon is lit, from dark through full and back.</summary>
public enum EnumMoonKind
{
    /// <summary>New moon, giving no light at all.</summary>
    Empty,

    /// <summary>Waxing, a sliver lit.</summary>
    Grow1,

    /// <summary>Waxing, half lit.</summary>
    Grow2,

    /// <summary>Waxing, nearly full.</summary>
    Grow3,

    /// <summary>Full moon, at its brightest.</summary>
    Full,

    /// <summary>Waning, nearly full.</summary>
    Shrink1,

    /// <summary>Waning, half lit.</summary>
    Shrink2,

    /// <summary>Waning, a sliver lit.</summary>
    Shrink3,
}

/// <summary>Which half of the world a place is in, which decides when its seasons fall.</summary>
public enum EnumHemisphereKind
{
    /// <summary>North of the equator.</summary>
    North,

    /// <summary>South of the equator, where the seasons are the other way round.</summary>
    South,
}

/// <summary>Which light a reading counts.</summary>
public enum EnumLightKind
{
    /// <summary>Light cast by blocks alone, ignoring the sky.</summary>
    OnlyBlockLight,

    /// <summary>Light from the sky alone, at its brightest rather than as it is now.</summary>
    OnlySunLight,

    /// <summary>Whichever of the two is brighter, ignoring the time of day.</summary>
    MaxLight,

    /// <summary>Whichever of the two is brighter, as it stands at this hour.</summary>
    MaxTimeOfDayLight,

    /// <summary>Light from the sky as it stands at this hour.</summary>
    TimeOfDaySunLight,

    /// <summary>How bright the sun itself is, rather than what it reaches.</summary>
    Sunbrightness,
}
