using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MoonTweaks.Scripting;

namespace MoonTweaks.Api;

/// <summary>
/// Reads Lua tables into spec objects using the same <see cref="LuaFieldAttribute"/>
/// metadata the reference documentation is generated from, so what a script may write
/// and what the docs promise cannot drift apart.
/// </summary>
public static class SpecBinder
{
    /// <summary>Binds a script value to a spec instance, reporting problems against <paramref name="origin"/>.</summary>
    public static object Bind(Type specType, ScriptValue value, ScriptOrigin origin, string path)
    {
        var table = TableAttributeOf(specType);
        var fields = FieldsOf(specType);

        if (value is ScriptValue.Str shorthand && table.Shorthand is { } shorthandField)
        {
            return BindEntries(specType, fields, new Dictionary<string, ScriptValue>
            {
                [shorthandField] = shorthand,
            }, origin, path);
        }

        if (value is not ScriptValue.Map map)
        {
            throw new ScriptError(origin,
                $"{path} expects a table{(table.Shorthand is null ? "" : " or a string")}, got {value.TypeName}");
        }

        return BindEntries(specType, fields, map.Entries, origin, path);
    }

    /// <summary>Assigns every known key and rejects the rest.</summary>
    private static object BindEntries(
        Type specType,
        IReadOnlyDictionary<string, PropertyInfo> fields,
        IReadOnlyDictionary<string, ScriptValue> entries,
        ScriptOrigin origin,
        string path)
    {
        var spec = Activator.CreateInstance(specType)!;

        foreach (var (key, entry) in entries)
        {
            if (!fields.TryGetValue(key, out var property))
            {
                throw new ScriptError(origin, $"{path} has no field '{key}'{Suggest(key, fields.Keys)}");
            }
            property.SetValue(spec, Convert(property.PropertyType, entry, origin, $"{path}.{key}"));
        }

        foreach (var key in RequiredOf(specType))
        {
            if (!entries.ContainsKey(key))
            {
                throw new ScriptError(origin, $"{path} is missing required field '{key}'");
            }
        }

        return spec;
    }

    /// <summary>
    /// Converts one script value to the CLR type asked for. Sole owner of that
    /// question: a table key and a function argument are the same problem, and
    /// answering it twice is how one of them ends up supporting fewer types.
    /// </summary>
    public static object? Convert(Type target, ScriptValue value, ScriptOrigin origin, string path)
    {
        var underlying = Nullable.GetUnderlyingType(target) ?? target;

        // A function the host will call back later, which no other target accepts.
        if (underlying == typeof(ScriptValue.Func))
        {
            return value is ScriptValue.Func handler
                ? handler
                : throw Expected(origin, path, "a function", value);
        }

        // A field the game stores as arbitrary data has no shape to bind against,
        // so the tree reaches the domain as the script wrote it and is converted there.
        if (underlying == typeof(ScriptValue)) return value;

        if (underlying == typeof(string))
        {
            return value is ScriptValue.Str s ? s.Value : throw Expected(origin, path, "a string", value);
        }

        if (underlying == typeof(int))
        {
            return value is ScriptValue.Num n ? (int)n.Value : throw Expected(origin, path, "a number", value);
        }

        if (underlying == typeof(double))
        {
            return value is ScriptValue.Num d ? d.Value : throw Expected(origin, path, "a number", value);
        }

        if (underlying == typeof(bool))
        {
            return value is ScriptValue.Bool b ? b.Value : throw Expected(origin, path, "a boolean", value);
        }

        if (underlying.IsEnum)
        {
            if (value is not ScriptValue.Str name) throw Expected(origin, path, "a string", value);
            var match = Enum.GetNames(underlying)
                .FirstOrDefault(candidate => candidate.Equals(name.Value, StringComparison.OrdinalIgnoreCase));
            if (match is null)
            {
                var allowed = string.Join(", ", Enum.GetNames(underlying).Select(n => $"'{n.ToLowerInvariant()}'"));
                throw new ScriptError(origin, $"{path} must be one of {allowed}, got '{name.Value}'");
            }
            return Enum.Parse(underlying, match);
        }

        // A list of values from a closed set, so a misspelling is refused by the same
        // path a single one would be.
        if (underlying.IsArray && underlying.GetElementType() is { IsEnum: true } choice)
        {
            var chosen = Items(value, origin, path, "a list");
            var picked = Array.CreateInstance(choice, chosen.Count);
            for (var index = 0; index < chosen.Count; index++)
            {
                picked.SetValue(Convert(choice, chosen[index], origin, $"{path}[{index + 1}]"), index);
            }
            return picked;
        }

        if (underlying == typeof(string[]))
        {
            return Items(value, origin, path, "a list of strings")
                .Select((item, index) => item is ScriptValue.Str s
                    ? s.Value
                    : throw Expected(origin, $"{path}[{index + 1}]", "a string", item))
                .ToArray();
        }

        if (underlying == typeof(string[][]))
        {
            var layers = Items(value, origin, path, "a list of rows");

            // Rows are the spelling; a shape with one layer is written as its rows
            // directly, so a list that starts with a string is read as that layer.
            if (layers.Count > 0 && layers[0] is ScriptValue.Str)
            {
                return new[] { (string[])Convert(typeof(string[]), value, origin, path)! };
            }

            return layers
                .Select((layer, index) =>
                    (string[])Convert(typeof(string[]), layer, origin, $"{path}[{index + 1}]")!)
                .ToArray();
        }

        // A list of table shapes, for the kinds that number their ingredients rather
        // than key them by a pattern character.
        if (underlying.IsArray
            && underlying.GetElementType() is { } element
            && DeclaredTable(element) is not null)
        {
            var entries = Items(value, origin, path, "a list");
            var bound = Array.CreateInstance(element, entries.Count);
            for (var index = 0; index < entries.Count; index++)
            {
                bound.SetValue(Bind(element, entries[index], origin, $"{path}[{index + 1}]"), index);
            }
            return bound;
        }

        if (underlying.IsGenericType && underlying.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            if (value is not ScriptValue.Map map) throw Expected(origin, path, "a table", value);
            var keyType = underlying.GetGenericArguments()[0];
            var elementType = underlying.GetGenericArguments()[1];
            var result = (System.Collections.IDictionary)Activator.CreateInstance(underlying)!;
            foreach (var (key, entry) in map.Entries)
            {
                // A table keyed by a closed set reads its keys through the same
                // conversion its values would use, so an unknown key is named.
                if (keyType.IsEnum)
                {
                    result[Convert(keyType, new ScriptValue.Str(key), origin, $"{path}.{key}")!] =
                        Convert(elementType, entry, origin, $"{path}.{key}");
                    continue;
                }

                // Converted rather than bound, so a table of numbers reads as readily
                // as a table of shapes; a shape still lands in the branch below.
                result[key] = Convert(elementType, entry, origin, $"{path}.{key}");
            }
            return result;
        }

        if (DeclaredTable(underlying) is not null)
        {
            return Bind(underlying, value, origin, path);
        }

        throw new ScriptError(origin, $"{path} has unsupported type {underlying.Name}");
    }

    /// <summary>
    /// What a value holds when a list was asked for. Sole owner of that question, so
    /// every list-shaped field answers it the same way.
    /// </summary>
    /// <remarks>
    /// Lua has one table type and writes both a list and a keyed table as <c>{}</c>,
    /// which leaves an empty one of each indistinguishable — the interpreter reads it
    /// as a keyed table, having no array part to go on. Emptiness is a thing scripts
    /// mean, though: no drops at all, no blocks left highlighted. So a target that
    /// wants a list takes an empty table as the empty list, and a target that wants a
    /// keyed table is unaffected, since it never reaches here.
    /// </remarks>
    private static IReadOnlyList<ScriptValue> Items(
        ScriptValue value, ScriptOrigin origin, string path, string expected) => value switch
    {
        ScriptValue.List list => list.Items,
        ScriptValue.Map { Entries.Count: 0 } => [],
        _ => throw Expected(origin, path, expected, value),
    };

    private static ScriptError Expected(ScriptOrigin origin, string path, string expected, ScriptValue got) =>
        new(origin, $"{path} expects {expected}, got {got.TypeName}");

    // What a shape is made of never changes while the mod is loaded, and reading it
    // costs a walk of the type's properties and an attribute lookup on each. Both
    // questions below are asked on paths a server takes constantly — every argument a
    // script fills in, and every table handed to a handler, which is once per event
    // raised and once per timer tick — so each type is read once and remembered.
    private static readonly ConcurrentDictionary<Type, IReadOnlyDictionary<string, PropertyInfo>> Fields = new();
    private static readonly ConcurrentDictionary<Type, LuaTableAttribute?> Tables = new();
    private static readonly ConcurrentDictionary<Type, IReadOnlyList<string>> Required = new();

    /// <summary>Every documented field of a spec, keyed by the name scripts write.</summary>
    public static IReadOnlyDictionary<string, PropertyInfo> FieldsOf(Type specType) =>
        Fields.GetOrAdd(specType, static type => type.GetProperties()
            .Where(property => property.GetCustomAttribute<LuaFieldAttribute>() is not null)
            .ToDictionary(property => property.GetCustomAttribute<LuaFieldAttribute>()!.Name));

    /// <summary>
    /// Keys a shape refuses to be built without, worked out once. Read from the same
    /// annotations as everything else, so a field made required above is required
    /// here without anything being told about it.
    /// </summary>
    /// <remarks>
    /// Remembered rather than asked each time for the reason the fields themselves
    /// are: reading an annotation builds the attribute object afresh on every call,
    /// and this asked once per field of every table a script writes.
    /// </remarks>
    private static IReadOnlyList<string> RequiredOf(Type specType) =>
        Required.GetOrAdd(specType, static type =>
            (IReadOnlyList<string>)FieldsOf(type)
                .Where(field => field.Value.GetCustomAttribute<LuaFieldAttribute>()!.Required)
                .Select(field => field.Key)
                .ToArray());

    /// <summary>
    /// The table annotation a type carries, or null where it is not a documented shape
    /// at all. Sole owner of that question, so what the binder reads a table into and
    /// what the writer hands one back as are decided the same way.
    /// </summary>
    public static LuaTableAttribute? DeclaredTable(Type type) =>
        Tables.GetOrAdd(type, static candidate => candidate.GetCustomAttribute<LuaTableAttribute>());

    /// <summary>The table annotation of a spec, which every spec is required to carry.</summary>
    public static LuaTableAttribute TableAttributeOf(Type specType) =>
        DeclaredTable(specType)
        ?? throw new InvalidOperationException($"{specType.Name} is not annotated with [LuaTable]");

    /// <summary>Offers the closest known field name when a script misspells one.</summary>
    private static string Suggest(string written, IEnumerable<string> known)
    {
        var closest = known
            .Select(candidate => (candidate, distance: Distance(written, candidate)))
            .Where(match => match.distance <= Math.Max(2, written.Length / 3))
            .OrderBy(match => match.distance)
            .Select(match => match.candidate)
            .FirstOrDefault();

        return closest is null ? "" : $"; did you mean '{closest}'?";
    }

    /// <summary>Levenshtein distance, used only to rank spelling suggestions.</summary>
    private static int Distance(string left, string right)
    {
        var previous = Enumerable.Range(0, right.Length + 1).ToArray();
        var current = new int[right.Length + 1];

        for (var i = 1; i <= left.Length; i++)
        {
            current[0] = i;
            for (var j = 1; j <= right.Length; j++)
            {
                var substitution = previous[j - 1] + (left[i - 1] == right[j - 1] ? 0 : 1);
                current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), substitution);
            }
            (previous, current) = (current, previous);
        }

        return previous[right.Length];
    }
}
