using System;

namespace MoonTweaks.Api;

/// <summary>Marks a class as a table reachable from Lua at <see cref="Path"/>.</summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class LuaModuleAttribute(string path) : Attribute
{
    /// <summary>Dotted path scripts use to reach this module, such as <c>moontweaks.recipes.grid</c>.</summary>
    public string Path { get; } = path;
}

/// <summary>Marks a method as callable from Lua.</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class LuaFunctionAttribute(string name) : Attribute
{
    /// <summary>Name the function is bound under inside its module.</summary>
    public string Name { get; } = name;
}

/// <summary>Marks a class as a table shape that scripts pass as an argument.</summary>
/// <remarks>
/// Not inherited: a shape that extends another names itself, so asking a derived
/// spec for its table finds one attribute rather than two.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class LuaTableAttribute(string name) : Attribute
{
    /// <summary>Name the shape is documented under.</summary>
    public string Name { get; } = name;

    /// <summary>
    /// Field a bare Lua string is assigned to, letting <c>"game:stick"</c> stand in
    /// for the whole table. Omitted when the shape has no such shorthand.
    /// </summary>
    public string? Shorthand { get; init; }

    /// <summary>
    /// Field a bare Lua list is assigned to, letting <c>{ "tool-axe" }</c> stand in
    /// for the whole table. Omitted when the shape has no such shorthand.
    /// </summary>
    /// <remarks>
    /// A list and a keyed table are the same Lua type, so a shape carrying this one
    /// is read as the shorthand only when the value the script wrote actually holds
    /// a list. A table written with keys still binds as itself.
    /// </remarks>
    public string? ListShorthand { get; init; }

    /// <summary>
    /// Whether the shape is handed to a script rather than written by one, as an
    /// event's table is. A given shape has no required keys and no defaults: every
    /// key is filled in before a handler sees it, and a key that may be absent says
    /// so by being nullable.
    /// </summary>
    public bool Given { get; init; }
}

/// <summary>Marks a property as a key of the enclosing Lua table.</summary>
/// <remarks>
/// Not inherited: a shape that restates a key to describe it differently carries
/// its own annotation, so asking for one finds a single answer.
/// </remarks>
[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class LuaFieldAttribute(string name) : Attribute
{
    /// <summary>Key scripts write in the table literal.</summary>
    public string Name { get; } = name;

    /// <summary>Whether omitting the key is an error.</summary>
    public bool Required { get; init; }

    /// <summary>Value used when the key is omitted, written as it would appear in Lua.</summary>
    public string? Default { get; init; }
}

/// <summary>
/// Names the set of values an editor offers for a table key or a function argument.
/// </summary>
/// <remarks>
/// The set only suggests, and replaces the type the member would otherwise be
/// documented as: every binder still reads whatever the CLR signature declares, so a
/// value outside the set is accepted. One annotation covers both kinds of member,
/// because a key and an argument naming the same thing should offer the same values.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, Inherited = false)]
public sealed class LuaSuggestsAttribute(string values) : Attribute
{
    /// <summary>Name of the set, which the generated reference declares as a type.</summary>
    public string Values { get; } = values;
}

/// <summary>
/// The sets <see cref="LuaSuggestsAttribute"/> names. An attribute argument has to be
/// a constant, so the names live here rather than beside the generator that emits the
/// declarations, and neither side can rename a set without the other following.
/// </summary>
public static class SuggestionSets
{
    /// <summary>Every code the item and block registries hold.</summary>
    public const string ASSET_CODE = "AssetCode";

    /// <summary>Every tag any item or block carries.</summary>
    public const string ASSET_TAG = "AssetTag";

    /// <summary>Every character trait a server's assets define.</summary>
    public const string ASSET_TRAIT = "AssetTrait";
}

/// <summary>
/// Names the shape of the table a handler is given, on the argument or the table key
/// the handler is passed as.
/// </summary>
/// <remarks>
/// A handler reaches the binder as a bare function, which says nothing about what it
/// will be called with. This names that shape, so an editor completes an event's keys
/// and the reference documents them, from the same class the host fills in.
/// </remarks>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, Inherited = false)]
public sealed class LuaPayloadAttribute(Type shape) : Attribute
{
    /// <summary>Class describing the table, which carries its own <see cref="LuaTableAttribute"/>.</summary>
    public Type Shape { get; } = shape;

    /// <summary>
    /// What the host does with whatever the handler hands back, written as the Lua
    /// type it accepts. Omitted where nothing is read, which is what an event does.
    /// </summary>
    public string? Returns { get; init; }
}
