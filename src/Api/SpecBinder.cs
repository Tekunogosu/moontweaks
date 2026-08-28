using System;
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

        foreach (var (key, property) in fields)
        {
            var field = property.GetCustomAttribute<LuaFieldAttribute>()!;
            if (field.Required && !entries.ContainsKey(key))
            {
                throw new ScriptError(origin, $"{path} is missing required field '{key}'");
            }
        }

        return spec;
    }

    /// <summary>Converts one script value to the CLR type the property declares.</summary>
    private static object? Convert(Type target, ScriptValue value, ScriptOrigin origin, string path)
    {
        var underlying = Nullable.GetUnderlyingType(target) ?? target;

        // A field the game stores as arbitrary data has no shape to bind against,
        // so the tree reaches the domain as it was written and is converted there.
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

        if (underlying == typeof(string[]))
        {
            if (value is not ScriptValue.List list) throw Expected(origin, path, "a list of strings", value);
            return list.Items
                .Select((item, index) => item is ScriptValue.Str s
                    ? s.Value
                    : throw Expected(origin, $"{path}[{index + 1}]", "a string", item))
                .ToArray();
        }

        if (underlying == typeof(string[][]))
        {
            if (value is not ScriptValue.List layers) throw Expected(origin, path, "a list of rows", value);

            // Rows are the spelling; a shape with one layer is written as its rows
            // directly, so a list that starts with a string is read as that layer.
            if (layers.Items.Count > 0 && layers.Items[0] is ScriptValue.Str)
            {
                return new[] { (string[])Convert(typeof(string[]), value, origin, path)! };
            }

            return layers.Items
                .Select((layer, index) =>
                    (string[])Convert(typeof(string[]), layer, origin, $"{path}[{index + 1}]")!)
                .ToArray();
        }

        // A list of table shapes, for the kinds that number their ingredients rather
        // than key them by a pattern character.
        if (underlying.IsArray
            && underlying.GetElementType() is { } element
            && element.GetCustomAttribute<LuaTableAttribute>() is not null)
        {
            if (value is not ScriptValue.List entries) throw Expected(origin, path, "a list", value);
            var bound = Array.CreateInstance(element, entries.Items.Count);
            for (var index = 0; index < entries.Items.Count; index++)
            {
                bound.SetValue(Bind(element, entries.Items[index], origin, $"{path}[{index + 1}]"), index);
            }
            return bound;
        }

        if (underlying.IsGenericType && underlying.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            if (value is not ScriptValue.Map map) throw Expected(origin, path, "a table", value);
            var elementType = underlying.GetGenericArguments()[1];
            var result = (System.Collections.IDictionary)Activator.CreateInstance(underlying)!;
            foreach (var (key, entry) in map.Entries)
            {
                result[key] = Bind(elementType, entry, origin, $"{path}.{key}");
            }
            return result;
        }

        if (underlying.GetCustomAttribute<LuaTableAttribute>() is not null)
        {
            return Bind(underlying, value, origin, path);
        }

        throw new ScriptError(origin, $"{path} has unsupported type {underlying.Name}");
    }

    private static ScriptError Expected(ScriptOrigin origin, string path, string expected, ScriptValue got) =>
        new(origin, $"{path} expects {expected}, got {got.TypeName}");

    /// <summary>Every documented field of a spec, keyed by the name scripts write.</summary>
    public static IReadOnlyDictionary<string, PropertyInfo> FieldsOf(Type specType) =>
        specType.GetProperties()
            .Where(property => property.GetCustomAttribute<LuaFieldAttribute>() is not null)
            .ToDictionary(property => property.GetCustomAttribute<LuaFieldAttribute>()!.Name);

    /// <summary>The table annotation of a spec, which every spec is required to carry.</summary>
    public static LuaTableAttribute TableAttributeOf(Type specType) =>
        specType.GetCustomAttribute<LuaTableAttribute>()
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
