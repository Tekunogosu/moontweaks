using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MoonTweaks.Api;

namespace MoonTweaks.DocGen;

/// <summary>
/// Reads the scriptable surface out of the mod assembly. It enumerates modules,
/// functions and fields through the same helpers the runtime binder uses, so the
/// reference cannot describe a surface the interpreter does not actually expose.
/// </summary>
public sealed class ApiReflector(Assembly assembly, XmlDocs docs)
{
    private readonly NullabilityInfoContext nullability = new();

    private readonly Dictionary<Type, string> tableNames = assembly.GetTypes()
        .Where(type => type.GetCustomAttribute<LuaTableAttribute>() is not null)
        .ToDictionary(type => type, type => type.GetCustomAttribute<LuaTableAttribute>()!.Name);

    /// <summary>Builds the complete model.</summary>
    public ApiModel Read(string version)
    {
        var modules = assembly.GetTypes()
            .Where(type => type.GetCustomAttribute<LuaModuleAttribute>() is not null)
            .Select(ReadModule)
            .OrderBy(module => module.Path, StringComparer.Ordinal)
            .ToList();

        var tables = tableNames.Keys
            .Select(ReadTable)
            .OrderBy(table => table.Name, StringComparer.Ordinal)
            .ToList();

        var enums = ReferencedEnums()
            .Select(ReadEnum)
            .OrderBy(value => value.Name, StringComparer.Ordinal)
            .ToList();

        return new ApiModel(version, modules, tables, enums);
    }

    private ModuleDoc ReadModule(Type type) => new(
        type.GetCustomAttribute<LuaModuleAttribute>()!.Path,
        docs.Summary(type),
        DomainBinder.FunctionsOf(type).Select(ReadFunction).ToList());

    private FunctionDoc ReadFunction(MethodInfo method) => new(
        method.GetCustomAttribute<LuaFunctionAttribute>()!.Name,
        docs.Summary(method),
        DomainBinder.ArgumentsOf(method)
            .Select(parameter => new ParameterDoc(
                parameter.Name ?? "?",
                Written(parameter),
                docs.Parameter(method, parameter.Name ?? "")))
            .ToList(),
        Returns(method));

    /// <summary>
    /// The type one argument is documented as. A handler names the shape it will be
    /// called with, so the table an event hands over completes as itself rather than
    /// as any table at all.
    /// </summary>
    private string Written(ParameterInfo parameter) =>
        Handler(parameter.GetCustomAttribute<LuaPayloadAttribute>())
        ?? Suggested(parameter.GetCustomAttribute<LuaSuggestsAttribute>(), parameter.ParameterType);

    /// <summary>
    /// A handler written as the call it will receive, when the member names the shape
    /// it is given. Null where the member is not a handler at all.
    /// </summary>
    private string? Handler(LuaPayloadAttribute? payload)
    {
        if (payload is null) return null;

        var answer = payload.Returns is null ? "" : $": {payload.Returns}";
        return $"fun(event: {LuaNameOf(payload.Shape)}){answer}";
    }

    /// <summary>
    /// What a function hands back. A binding that returns the neutral value tree
    /// returns whatever it was asked for rather than a table in particular, which is
    /// the same type read as an argument but not the same promise.
    /// </summary>
    private string Returns(MethodInfo method)
    {
        if (method.ReturnType == typeof(void)) return "nil";
        if (method.ReturnType == typeof(MoonTweaks.Scripting.ScriptValue)) return "any";
        return LuaNameOf(method.ReturnType);
    }

    private TableDoc ReadTable(Type type)
    {
        var attribute = type.GetCustomAttribute<LuaTableAttribute>()!;
        var fields = SpecBinder.FieldsOf(type)
            .Select(entry =>
            {
                var field = entry.Value.GetCustomAttribute<LuaFieldAttribute>()!;
                return new FieldDoc(
                    field.Name,
                    Handler(entry.Value.GetCustomAttribute<LuaPayloadAttribute>())
                    ?? Suggested(entry.Value.GetCustomAttribute<LuaSuggestsAttribute>(), entry.Value.PropertyType),
                    attribute.Given ? AlwaysPresent(entry.Value) : field.Required,
                    attribute.Given ? null : field.Default,
                    docs.Summary(entry.Value));
            })
            .OrderByDescending(field => field.Required)
            .ThenBy(field => field.Name, StringComparer.Ordinal)
            .ToList();

        return new TableDoc(attribute.Name, docs.Summary(type), attribute.Shorthand, attribute.Given, fields);
    }

    /// <summary>
    /// Whether a key of a given shape is never nil, which its declared nullability
    /// says. Nothing omits a key of a table the host writes, so what a script has to
    /// guard against is the value rather than the key's absence.
    /// </summary>
    private bool AlwaysPresent(PropertyInfo property) =>
        Nullable.GetUnderlyingType(property.PropertyType) is null
        && nullability.Create(property).ReadState != NullabilityState.Nullable;

    private EnumDoc ReadEnum(Type type) => new(
        type.Name,
        docs.Summary(type),
        Enum.GetNames(type)
            .Select(name => new EnumValueDoc(name.ToLowerInvariant(), docs.Summary(type, name)))
            .ToList());

    /// <summary>
    /// Every enumeration reachable from a documented field, whether the field is one
    /// of them, a list of them, or a table keyed by them. A set that is declared but
    /// never reached this way would go undocumented, and a field pointing at an
    /// undeclared set would leave an editor with a type it cannot resolve.
    /// </summary>
    private IEnumerable<Type> ReferencedEnums() => FieldTypes()
        .Concat(FunctionTypes())
        .SelectMany(type => Within(Nullable.GetUnderlyingType(type) ?? type))
        .Where(type => type.IsEnum)
        .Distinct();

    /// <summary>Every type a table key is written as.</summary>
    private IEnumerable<Type> FieldTypes() => tableNames.Keys
        .SelectMany(type => SpecBinder.FieldsOf(type).Values)
        .Select(property => property.PropertyType);

    /// <summary>
    /// Every type a function takes or hands back. A set that only ever appears on a
    /// function would otherwise go undeclared, leaving an editor with a name it
    /// cannot resolve on a surface that reads perfectly well in the source.
    /// </summary>
    private IEnumerable<Type> FunctionTypes() => assembly.GetTypes()
        .Where(type => type.GetCustomAttribute<LuaModuleAttribute>() is not null)
        .SelectMany(DomainBinder.FunctionsOf)
        .SelectMany(method => DomainBinder.ArgumentsOf(method)
            .Select(parameter => parameter.ParameterType)
            .Append(method.ReturnType));

    /// <summary>The types a field reaches: itself, what it holds, and what it is keyed by.</summary>
    private static IEnumerable<Type> Within(Type type)
    {
        yield return type;

        if (type.IsArray && type.GetElementType() is { } element) yield return element;

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            foreach (var argument in type.GetGenericArguments()) yield return argument;
        }
    }

    /// <summary>
    /// The type a table key or a function argument is documented as. A suggestion
    /// names the values one element may take, so a member holding a list of them is
    /// still written as a list.
    /// </summary>
    private string Suggested(LuaSuggestsAttribute? suggests, Type type)
    {
        if (suggests is null) return LuaNameOf(type);
        return type.IsArray ? $"{suggests.Values}[]" : suggests.Values;
    }

    /// <summary>Renders a CLR type as the Lua type scripts actually write.</summary>
    private string LuaNameOf(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type);
        if (underlying is not null) return LuaNameOf(underlying) + "?";

        if (type == typeof(string)) return "string";
        if (type == typeof(int)) return "integer";
        if (type == typeof(double)) return "number";
        if (type == typeof(bool)) return "boolean";
        if (type == typeof(float)) return "number";
        if (type == typeof(void)) return "nil";
        // Arbitrary data: the binder constrains nothing, so neither does the type.
        if (type == typeof(MoonTweaks.Scripting.ScriptValue)) return "table";
        // A handler the host calls back, given one table describing what happened.
        if (type == typeof(MoonTweaks.Scripting.ScriptValue.Func)) return "fun(event: table)";
        if (type == typeof(string[])) return "string[]";
        // Written as rows, or as a list of them when a shape has more than one layer.
        if (type == typeof(string[][])) return "string[] | string[][]";
        if (tableNames.TryGetValue(type, out var table)) return table;
        if (type.IsArray && type.GetElementType() is { } element)
        {
            if (tableNames.TryGetValue(element, out var each)) return $"{each}[]";
            if (element.IsEnum) return $"{element.Name}[]";
        }
        if (type.IsEnum) return type.Name;

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            var arguments = type.GetGenericArguments();
            return $"table<{LuaNameOf(arguments[0])}, {LuaNameOf(arguments[1])}>";
        }

        return type.Name;
    }
}
