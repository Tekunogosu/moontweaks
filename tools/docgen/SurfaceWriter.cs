using System;
using System.Linq;
using System.Text;

namespace MoonTweaks.DocGen;

/// <summary>
/// Writes the checklist the diagnostics suite measures its coverage against: every
/// bound function, named as a script names it. Generated rather than kept by hand,
/// because a checklist that has fallen behind the bindings reports full coverage of
/// the functions it happens to list and says nothing about the ones it does not.
/// </summary>
public static class SurfaceWriter
{
    /// <summary>Prefix every module path carries, which a script does not repeat.</summary>
    private const string ROOT = "moontweaks.";

    /// <summary>Renders the checklist, ordered so a diff between builds reads as a change.</summary>
    public static string Write(ApiModel api)
    {
        var output = new StringBuilder();
        output.AppendLine("-- Every function this mod binds, as the report's checklist.");
        output.AppendLine("--");
        output.AppendLine("-- Generated from the same reference the editor library is generated from, so a");
        output.AppendLine("-- function bound later shows up here as untouched rather than going unnoticed. The");
        output.AppendLine("-- suite marks each name as it exercises it, and `/diag report` names whatever is");
        output.AppendLine("-- left, which is what makes the coverage figure a measurement rather than a claim.");
        output.AppendLine("--");
        output.AppendLine("-- Regenerate with `scripts/docs.sh`; do not edit.");
        output.AppendLine();
        output.AppendLine("diag.surface = {");

        foreach (var name in Names(api)) output.AppendLine($"  \"{name}\",");

        output.AppendLine("}");
        return output.ToString();
    }

    /// <summary>Every bound function as a script writes it, sorted by that name.</summary>
    private static string[] Names(ApiModel api) =>
        api.Modules
            .SelectMany(module => module.Functions
                .Select(function => $"{module.Path[ROOT.Length..]}.{function.Name}"))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
}
