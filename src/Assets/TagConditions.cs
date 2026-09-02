using System;
using System.Linq;
using MoonTweaks.Api;
using MoonTweaks.Scripting;
using Vintagestory.API.Datastructures;

namespace MoonTweaks.Assets;

/// <summary>
/// The tags a script named, translated into the condition the game matches an asset
/// against. Sole owner of that grammar, so an ingredient, a property change and a
/// recipe selector all read the same condition the same way.
/// </summary>
/// <remarks>
/// The game holds a condition as a list of groups and one flag deciding how they
/// combine, and that flag also decides what a group means. Disjunctive: any one
/// group matching is enough, and a group asks that every tag it requires be carried.
/// Conjunctive: every group must match, and a group asks only that one of its tags
/// be carried. So the two halves of a junction are not free to vary — a junction of
/// <c>allOf</c> groups is written with <c>anyOf</c> inside it and the other way
/// round, which is the game's own rule that junction verbs alternate by layer.
/// </remarks>
public static class TagConditions
{
    /// <summary>Whether a condition holds every group, or any one of them.</summary>
    private const bool ANY_GROUP = true;
    private const bool EVERY_GROUP = false;

    /// <summary>
    /// Reads what a script wrote, refusing every way of writing a condition that
    /// would match nothing at all. An absent condition is the empty one, which is
    /// what makes tags optional beside a code.
    /// </summary>
    /// <param name="registry">Registry that knows which tag names exist.</param>
    /// <param name="spec">Condition the script wrote, if it wrote one.</param>
    /// <param name="origin">Script line that wrote it.</param>
    /// <param name="path">
    /// Where the condition itself sits, as a failure should name it — the whole path
    /// including the key, not the shape holding it.
    /// </param>
    public static ComplexTagCondition<TSet> Build<TSet>(
        ITagRegistry<TSet> registry, TagConditionSpec? spec, ScriptOrigin origin, string path)
        where TSet : struct, IEquatable<TSet>
    {
        if (spec is null) return default;

        if (spec is { AllOf: not null, AnyOf: not null })
        {
            throw new ScriptError(origin,
                $"{path} names both 'allOf' and 'anyOf', and a condition combines one junction at a time");
        }

        return (spec.AllOf, spec.AnyOf) switch
        {
            ({ Names: { } every }, _) => Single(registry, every, spec.NoneOf, ANY_GROUP, origin, path, "allOf"),
            (_, { Names: { } some }) => Single(registry, some, spec.NoneOf, EVERY_GROUP, origin, path, "anyOf"),
            ({ Groups: { } all }, _) => Groups(registry, all, spec, EVERY_GROUP, origin, path, "allOf"),
            (_, { Groups: { } any }) => Groups(registry, any, spec, ANY_GROUP, origin, path, "anyOf"),
            _ when spec.NoneOf is { Length: > 0 } forbidden =>
                Single(registry, [], forbidden, ANY_GROUP, origin, path, "noneOf"),
            _ => throw new ScriptError(origin,
                $"{path} names no tags, so nothing would be selected by it"),
        };
    }

    /// <summary>
    /// One group standing on its own, as a junction of bare names is.
    /// </summary>
    /// <remarks>
    /// Read with the junction the names were written under: required tags mean
    /// "carries all of these" in a disjunctive condition and "carries one of these"
    /// in a conjunctive one, so the same group says <c>allOf</c> under the first flag
    /// and <c>anyOf</c> under the second.
    ///
    /// A lone <c>noneOf</c> is built disjunctive for the same reason. The game's
    /// converter leaves the flag at its default there, which asks a group with
    /// nothing required to overlap an asset's tags and so matches nothing at all;
    /// built this way it means what it says.
    /// </remarks>
    private static ComplexTagCondition<TSet> Single<TSet>(
        ITagRegistry<TSet> registry,
        string[] required,
        string[]? forbidden,
        bool junction,
        ScriptOrigin origin,
        string path,
        string key)
        where TSet : struct, IEquatable<TSet>
    {
        // An empty junction asks for nothing, which the game reads as matching
        // everything under one flag and nothing under the other. Neither is what a
        // script writing an empty list meant by it.
        if (key != "noneOf" && required.Length == 0)
        {
            throw new ScriptError(origin, $"{path}.{key} names no tags, so nothing would be selected by it");
        }

        return new ComplexTagCondition<TSet>
        {
            conditions =
            [
                new ComplexTagCondition<TSet>.Condition
                {
                    RequiredTags = Set(registry, required, origin, $"{path}.{key}"),
                    ForbiddenTags = Set(registry, forbidden ?? [], origin, $"{path}.noneOf"),
                },
            ],
            isDisjunctive = junction,
        };
    }

    /// <summary>Several groups, each asking with the key its junction leaves it.</summary>
    /// <remarks>
    /// A group carries its own forbidden tags, so one written beside the junction has
    /// no group to belong to and is refused rather than quietly applied to all of
    /// them. The game refuses it in the same place and for the same reason.
    /// </remarks>
    private static ComplexTagCondition<TSet> Groups<TSet>(
        ITagRegistry<TSet> registry,
        TagGroupSpec[] groups,
        TagConditionSpec spec,
        bool junction,
        ScriptOrigin origin,
        string path,
        string key)
        where TSet : struct, IEquatable<TSet>
    {
        if (spec.NoneOf is not null)
        {
            throw new ScriptError(origin,
                $"{path} names 'noneOf' beside groups, which each carry their own; "
                + $"move it into the groups of '{key}'");
        }

        if (groups.Length == 0)
        {
            throw new ScriptError(origin, $"{path}.{key} holds no groups, so nothing would be selected by it");
        }

        // The verb a group asks with is the other one: a junction of groups any of
        // which matches asks each group for everything, and one every group matches
        // asks each group for anything.
        var asked = junction == ANY_GROUP ? "allOf" : "anyOf";

        return new ComplexTagCondition<TSet>
        {
            conditions = [.. groups.Select((group, index) =>
                Group(registry, group, asked, origin, $"{path}.{key}[{index + 1}]"))],
            isDisjunctive = junction,
        };
    }

    /// <summary>One group of a junction, which must ask with the key that junction leaves it.</summary>
    private static ComplexTagCondition<TSet>.Condition Group<TSet>(
        ITagRegistry<TSet> registry,
        TagGroupSpec group,
        string asked,
        ScriptOrigin origin,
        string path)
        where TSet : struct, IEquatable<TSet>
    {
        var written = asked == "allOf" ? group.AllOf : group.AnyOf;
        var other = asked == "allOf" ? group.AnyOf : group.AllOf;

        if (written is not null && other is not null)
        {
            throw new ScriptError(origin,
                $"{path} names both 'allOf' and 'anyOf', and a group asks with one of them");
        }

        // Asking with the junction's own key is the mistake worth spelling out, since
        // it is what somebody writing the other junction reaches for first.
        if (written is null && other is not null)
        {
            var junction = asked == "allOf" ? "anyOf" : "allOf";

            throw new ScriptError(origin,
                $"{path} asks with '{junction}' inside an '{junction}', which combines groups that each "
                + $"ask with '{asked}'; a set of '{junction}' groups belongs under '{asked}' instead");
        }

        if (written is null or { Length: 0 })
        {
            throw new ScriptError(origin, $"{path} names no tags, so nothing would be selected by it");
        }

        return new ComplexTagCondition<TSet>.Condition
        {
            RequiredTags = Set(registry, written, origin, $"{path}.{asked}"),
            ForbiddenTags = Set(registry, group.NoneOf ?? [], origin, $"{path}.noneOf"),
        };
    }

    /// <summary>
    /// Tag names as the set the game matches with. Sole owner of naming a tag the
    /// server has never heard of, which the registry reports rather than guesses at,
    /// so a misspelling names itself instead of silently matching nothing.
    /// </summary>
    private static TSet Set<TSet>(
        ITagRegistry<TSet> registry, string[] names, ScriptOrigin origin, string path)
        where TSet : struct, IEquatable<TSet>
    {
        // The empty set is the default one for both of the game's set types, which is
        // what lets this be written without naming either.
        if (names.Length == 0) return default;

        var error = registry.TryCreateTagSet(out var set, names);
        if (error == TagRegistryError.None) return set;

        var unknown = names
            .Where(tag => registry.TryCreateTagSet(out _, [tag]) != TagRegistryError.None)
            .ToList();

        throw new ScriptError(origin, unknown.Count > 0
            ? $"{path} names {string.Join(", ", unknown.Select(tag => $"'{tag}'"))}, "
              + "which no item or block carries"
            : $"{path} could not be read ({error})");
    }
}
