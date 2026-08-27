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
                LuaNameOf(parameter.ParameterType),
                docs.Parameter(method, parameter.Name ?? "")))
            .ToList(),
        method.ReturnType == typeof(void) ? "nil" : LuaNameOf(method.ReturnType));

    private TableDoc ReadTable(Type type)
    {
        var attribute = type.GetCustomAttribute<LuaTableAttribute>()!;
        var fields = SpecBinder.FieldsOf(type)
            .Select(entry =>
            {
                var field = entry.Value.GetCustomAttribute<LuaFieldAttribute>()!;
                return new FieldDoc(
                    field.Name,
                    Suggested(field, entry.Value.PropertyType),
                    field.Required,
                    field.Default,
                    docs.Summary(entry.Value));
            })
            .OrderByDescending(field => field.Required)
            .ThenBy(field => field.Name, StringComparer.Ordinal)
            .ToList();

        return new TableDoc(attribute.Name, docs.Summary(type), attribute.Shorthand, fields);
    }

    private EnumDoc ReadEnum(Type type) => new(
        type.Name,
        docs.Summary(type),
        Enum.GetNames(type)
            .Select(name => new EnumValueDoc(name.ToLowerInvariant(), docs.Summary(type, name)))
            .ToList());

    /// <summary>Every enumeration reachable from a documented field or parameter.</summary>
    private IEnumerable<Type> ReferencedEnums() => tableNames.Keys
        .SelectMany(type => SpecBinder.FieldsOf(type).Values)
        .Select(property => Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType)
        .Where(type => type.IsEnum)
        .Distinct();

    /// <summary>
    /// The type a field is documented as. A suggestion names the values one element
    /// may take, so a field holding a list of them is still written as a list.
    /// </summary>
    private string Suggested(LuaFieldAttribute field, Type type)
    {
        if (field.Suggests is not { } values) return LuaNameOf(type);
        return type.IsArray ? $"{values}[]" : values;
    }

    /// <summary>Renders a CLR type as the Lua type scripts actually write.</summary>
    private string LuaNameOf(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type);
        if (underlying is not null) return LuaNameOf(underlying) + "?";

        if (type == typeof(string)) return "string";
        if (type == typeof(int) || type == typeof(double)) return "integer";
        if (type == typeof(bool)) return "boolean";
        if (type == typeof(void)) return "nil";
        if (type == typeof(string[])) return "string[]";
        // Written as rows, or as a list of them when a shape has more than one layer.
        if (type == typeof(string[][])) return "string[] | string[][]";
        if (tableNames.TryGetValue(type, out var table)) return table;
        if (type.IsEnum) return type.Name;

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            var arguments = type.GetGenericArguments();
            return $"table<{LuaNameOf(arguments[0])}, {LuaNameOf(arguments[1])}>";
        }

        return type.Name;
    }
}
