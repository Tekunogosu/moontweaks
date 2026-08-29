namespace MoonTweaks.Api;

// What a script writes to put something into the world.

/// <summary>
/// How fast something is moving and which way. Left out entirely, a dropped stack
/// simply falls where it was put.
/// </summary>
/// <remarks>
/// Measured per physics step rather than per second, so the numbers are far smaller
/// than the distance travelled suggests. The game throws a stack a player drops at
/// roughly <c>0.1</c>, which lands it a step away; <c>1</c> is a hard fling and
/// anything much beyond that leaves the map. Multiply what
/// <c>moontweaks.players.facing</c> gives by the speed wanted rather than guessing
/// at each part separately.
/// </remarks>
[LuaTable("Velocity")]
public sealed class VelocitySpec
{
    /// <summary>East for a positive number, west for a negative one.</summary>
    [LuaField("x", Default = "0")]
    public double X { get; set; }

    /// <summary>Upwards for a positive number. A little of this makes a stack arc rather than skid.</summary>
    [LuaField("y", Default = "0")]
    public double Y { get; set; }

    /// <summary>South for a positive number, north for a negative one.</summary>
    [LuaField("z", Default = "0")]
    public double Z { get; set; }
}

/// <summary>A stack put into the world as a thing lying on the ground.</summary>
[LuaTable("Drop")]
public sealed class DropSpec
{
    /// <summary>What to drop.</summary>
    [LuaField("stack", Required = true)]
    public ItemStackSpec Stack { get; set; } = new();

    /// <summary>Where to drop it, east to west.</summary>
    [LuaField("x", Required = true)]
    public double X { get; set; }

    /// <summary>Where to drop it, from the world's floor upwards.</summary>
    [LuaField("y", Required = true)]
    public double Y { get; set; }

    /// <summary>Where to drop it, north to south.</summary>
    [LuaField("z", Required = true)]
    public double Z { get; set; }

    /// <summary>
    /// How fast to throw it, and which way. Left out, it appears where it was put and
    /// falls straight down, which lands it close enough to walk back into.
    /// </summary>
    [LuaField("velocity")]
    public VelocitySpec? Velocity { get; set; }

    /// <summary>
    /// Player this came from, who then cannot pick it up for a second. This is what
    /// stops a stack dropped at somebody's feet going straight back into their hands,
    /// and it is what the game does when a player throws something down. Left out,
    /// anybody standing there collects it at once.
    /// </summary>
    [LuaField("owner")]
    public string? Owner { get; set; }
}
