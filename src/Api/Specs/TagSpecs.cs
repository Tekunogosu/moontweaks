namespace MoonTweaks.Api;

// What a script writes to select on what a thing is rather than on what it is
// called. The game's own recipe files spell this with allOf, anyOf and noneOf, and
// so does this: a condition ported from JSON reads the same here as it did there.

/// <summary>
/// Which tags a match must carry. A bare list names the tags every match carries,
/// so <c>{ "tool-axe" }</c> is the whole shape for the common case; the keys below
/// are for the conditions a list cannot say.
/// </summary>
/// <remarks>
/// One junction at a time: naming both <c>allOf</c> and <c>anyOf</c> here is
/// refused, the same way the game refuses it. Each may hold tag names or the groups
/// those names sit in, and which one is meant is read off what the list holds.
/// </remarks>
[LuaTable("TagCondition", ListShorthand = "allOf")]
public sealed class TagConditionSpec
{
    /// <summary>
    /// Tags every match carries, or the groups every match satisfies. Written as
    /// names — <c>{ "tool", "weapon-melee" }</c> — or as groups, each of which asks
    /// for <c>anyOf</c>.
    /// </summary>
    [LuaField("allOf")]
    public TagJunction? AllOf { get; set; }

    /// <summary>
    /// Tags a match carries at least one of, or the groups a match satisfies at
    /// least one of. Written as names — <c>{ "tool-axe", "tool-pickaxe" }</c> — or
    /// as groups, each of which asks for <c>allOf</c>.
    /// </summary>
    [LuaField("anyOf")]
    public TagJunction? AnyOf { get; set; }

    /// <summary>
    /// Tags no match carries. Used beside a junction of names, or alone to select on
    /// what something is not. Groups carry their own, so a junction of groups
    /// refuses one here.
    /// </summary>
    [LuaField("noneOf")]
    [LuaSuggests(SuggestionSets.ASSET_TAG)]
    public string[]? NoneOf { get; set; }
}

/// <summary>
/// One group of tags inside a junction. Which key a group asks with is decided by
/// the junction holding it, since a group is what that junction combines: groups
/// under <c>anyOf</c> ask with <c>allOf</c>, and groups under <c>allOf</c> ask with
/// <c>anyOf</c>.
/// </summary>
[LuaTable("TagGroup")]
public sealed class TagGroupSpec
{
    /// <summary>Tags every match in this group carries. Written inside an <c>anyOf</c>.</summary>
    [LuaField("allOf")]
    [LuaSuggests(SuggestionSets.ASSET_TAG)]
    public string[]? AllOf { get; set; }

    /// <summary>Tags a match in this group carries at least one of. Written inside an <c>allOf</c>.</summary>
    [LuaField("anyOf")]
    [LuaSuggests(SuggestionSets.ASSET_TAG)]
    public string[]? AnyOf { get; set; }

    /// <summary>Tags no match in this group carries.</summary>
    [LuaField("noneOf")]
    [LuaSuggests(SuggestionSets.ASSET_TAG)]
    public string[]? NoneOf { get; set; }
}

/// <summary>
/// What one side of a junction holds: tag names, or the groups those names sit in.
/// Exactly one of the two is filled in, by whichever the script wrote.
/// </summary>
/// <remarks>
/// Not a table shape of its own — it is one key read two ways, which is how the
/// game's own converter reads it, and the binder tells them apart by what the first
/// item of the list is.
/// </remarks>
public sealed class TagJunction
{
    /// <summary>Tag names, where the list held those.</summary>
    public string[]? Names { get; init; }

    /// <summary>Groups, where the list held those instead.</summary>
    public TagGroupSpec[]? Groups { get; init; }
}
