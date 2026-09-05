using System;
using System.Linq;
using System.Text;
using MoonTweaks.Host;

namespace MoonTweaks.Reference;

/// <summary>
/// Writes LuaCATS annotations, the format lua-language-server reads. Dropping the
/// result into a pack's workspace gives authors completion and type checking on
/// the same surface the reference describes.
/// </summary>
public static class LuaCatsWriter
{
    /// <summary>Renders the whole API as a definitions file.</summary>
    public static string Write(ApiModel api)
    {
        var body = Body(api);
        var output = new StringBuilder();
        output.AppendLine("---@meta");
        output.AppendLine($"--- {api.Name} {api.Version} scripting API.");
        output.AppendLine("--- Generated from the mod's bindings; do not edit.");
        // Everything the header does not itself contain, so a reader of one line
        // can tell whether a file on disk is the one this build would write.
        output.AppendLine($"{LibraryHeader.BUILD_MARKER}{LibraryHeader.Fingerprint(api.Name + api.Version + body)}");
        output.AppendLine();
        output.Append(body);
        return output.ToString();
    }

    /// <summary>Everything below the header: the types, then the modules.</summary>
    private static string Body(ApiModel api)
    {
        var output = new StringBuilder();

        foreach (var enumeration in api.Enums)
        {
            Comment(output, enumeration.Summary);
            var values = string.Join(" | ", enumeration.Values.Select(value => $"\"{value.Name}\""));
            output.AppendLine($"---@alias {enumeration.Name} {values}");
            foreach (var value in enumeration.Values)
            {
                output.AppendLine($"--- - `\"{value.Name}\"` {value.Summary}");
            }
            output.AppendLine();
        }

        foreach (var table in api.Tables)
        {
            // A shape that accepts a shorthand is a union, so its public name becomes
            // an alias over the spellings and the fields hang off a private class.
            var shorthands = new[] { table.Shorthand, table.ListShorthand }
                .Where(field => field is not null)
                .Select(field => field!)
                .ToList();
            var shape = shorthands.Count == 0 ? table.Name : $"{table.Name}Table";

            Comment(output, table.Summary);
            if (shorthands.Count > 0)
            {
                // A shorthand stands in for one field, so it is written as whatever
                // that field is: a code field keeps the values an editor suggests for
                // it rather than widening to a bare string.
                if (table.Shorthand is not null)
                {
                    output.AppendLine($"--- A bare string is shorthand for `{{ {table.Shorthand} = <string> }}`.");
                }
                if (table.ListShorthand is not null)
                {
                    output.AppendLine($"--- A bare list is shorthand for `{{ {table.ListShorthand} = <list> }}`.");
                }
                var written = shorthands.Select(field =>
                    table.Fields.FirstOrDefault(candidate => candidate.Name == field)?.Type ?? "string");
                output.AppendLine($"---@alias {table.Name} {string.Join(" | ", written.Prepend(shape))}");
                output.AppendLine();
                Comment(output, table.Summary);
            }
            output.AppendLine($"---@class {shape}");
            foreach (var field in table.Fields)
            {
                // The optional marker already says the field may be absent, so a
                // nullable type would repeat it.
                var optional = field.Required ? "" : "?";
                var type = field.Type.TrimEnd('?');
                var note = field.Default is null ? "" : $" Defaults to `{field.Default}`.";
                output.AppendLine($"---@field {field.Name}{optional} {type} {field.Summary}{note}");
            }
            output.AppendLine();
        }

        foreach (var module in api.Modules)
        {
            Declare(output, module.Path);
            Comment(output, module.Summary);
            // An editor renders a doc comment as markdown, so the example arrives on
            // hover looking like the one on the reference page rather than as prose.
            if (module.Example.Length > 0)
            {
                output.AppendLine("---");
                output.AppendLine("--- ```lua");
                foreach (var line in module.Example.Split('\n')) output.AppendLine($"--- {line}".TrimEnd());
                output.AppendLine("--- ```");
            }
            output.AppendLine($"{module.Path} = {{}}");
            output.AppendLine();

            foreach (var function in module.Functions)
            {
                Comment(output, function.Summary);
                foreach (var parameter in function.Parameters)
                {
                    // A nilable type on a parameter is a parameter that may be left
                    // out, which LuaCATS spells on the name rather than on the type.
                    var optional = parameter.Type.EndsWith('?');
                    var name = optional ? parameter.Name + "?" : parameter.Name;
                    var type = optional ? parameter.Type[..^1] : parameter.Type;

                    output.AppendLine($"---@param {name} {type} {parameter.Summary}");
                }
                if (function.Returns != "nil") output.AppendLine($"---@return {function.Returns}");

                var arguments = string.Join(", ", function.Parameters.Select(parameter => parameter.Name));
                output.AppendLine($"function {module.Path}.{function.Name}({arguments}) end");
                output.AppendLine();
            }
        }

        return output.ToString();
    }

    /// <summary>Emits the parent tables a dotted path needs before it can be assigned.</summary>
    private static void Declare(StringBuilder output, string path)
    {
        var segments = path.Split('.');
        for (var i = 1; i < segments.Length; i++)
        {
            var parent = string.Join('.', segments[..i]);
            if (!output.ToString().Contains($"{parent} = ", StringComparison.Ordinal))
            {
                output.AppendLine($"{parent} = {parent} or {{}}");
            }
        }
    }

    private static void Comment(StringBuilder output, string summary)
    {
        if (!string.IsNullOrWhiteSpace(summary)) output.AppendLine($"--- {summary}");
    }
}
