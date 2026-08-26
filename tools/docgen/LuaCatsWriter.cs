using System;
using System.Linq;
using System.Text;

namespace MoonTweaks.DocGen;

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
        var output = new StringBuilder();
        output.AppendLine("---@meta");
        output.AppendLine($"--- MoonTweaks {api.Version} scripting API.");
        output.AppendLine("--- Generated from the mod's bindings; do not edit.");
        output.AppendLine();

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
            Comment(output, table.Summary);
            if (table.Shorthand is not null)
            {
                output.AppendLine($"--- A bare string is shorthand for `{{ {table.Shorthand} = <string> }}`.");
            }
            output.AppendLine($"---@class {table.Name}");
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
            output.AppendLine($"{module.Path} = {{}}");
            output.AppendLine();

            foreach (var function in module.Functions)
            {
                Comment(output, function.Summary);
                foreach (var parameter in function.Parameters)
                {
                    output.AppendLine($"---@param {parameter.Name} {parameter.Type} {parameter.Summary}");
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
