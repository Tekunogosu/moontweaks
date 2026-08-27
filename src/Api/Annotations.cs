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

    /// <summary>
    /// Named set of values an editor offers for this key, replacing the type it
    /// would otherwise be documented as. The set only suggests: the binder still
    /// reads whatever the property declares, so a value outside it is accepted.
    /// </summary>
    public string? Suggests { get; init; }
}
