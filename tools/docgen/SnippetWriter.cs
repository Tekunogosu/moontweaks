using System.Linq;
using System.Text;

namespace MoonTweaks.DocGen;

/// <summary>
/// Writes every module's example into one Lua file, so the snippets the reference
/// shows are checked by `lua-language-server` against the same library an author
/// writes against. A documentation example that no longer compiles is the only kind
/// worth less than none, and this is what stops one reaching the site.
/// </summary>
public static class SnippetWriter
{
    /// <summary>Renders every example, each in a scope of its own.</summary>
    public static string Write(ApiModel api)
    {
        var output = new StringBuilder();
        output.AppendLine("-- Every module's worked example, gathered where the language server checks it.");
        output.AppendLine("-- Generated from the doc comments beside the bindings; do not edit.");

        foreach (var module in api.Modules.Where(module => module.Example.Length > 0))
        {
            output.AppendLine();
            // A block apiece, so one example's names cannot reach another's and each
            // reads exactly as it does on the page.
            output.AppendLine($"do -- {module.Path}");
            foreach (var line in module.Example.Split('\n'))
            {
                output.AppendLine(line.Length == 0 ? "" : $"  {line}");
            }
            output.AppendLine("end");
        }

        return output.ToString();
    }
}
