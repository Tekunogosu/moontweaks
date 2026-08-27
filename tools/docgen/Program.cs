using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using MoonTweaks.DocGen;

if (args.Length < 4)
{
    Console.Error.WriteLine(
        "usage: moontweaks-docgen <assembly.dll> <assembly.xml> <out-dir> <version> [--check]");
    return 2;
}

var (assemblyPath, xmlPath, outputDir, version) = (args[0], args[1], args[2], args[3]);
var checkOnly = args.Contains("--check");

var assembly = Assembly.LoadFrom(Path.GetFullPath(assemblyPath));
var api = new ApiReflector(assembly, XmlDocs.Load(xmlPath)).Read(version);

var undocumented = Undocumented(api).ToList();
foreach (var gap in undocumented) Console.Error.WriteLine($"undocumented: {gap}");

if (undocumented.Count > 0)
{
    Console.Error.WriteLine(
        $"{undocumented.Count} undocumented member(s); add an XML doc comment beside the binding.");
    return 1;
}

if (checkOnly)
{
    Console.WriteLine($"all {Members(api)} member(s) documented");
    return 0;
}

Directory.CreateDirectory(outputDir);
Directory.CreateDirectory(Path.Combine(outputDir, "library"));

Write(Path.Combine(outputDir, "api.json"),
    JsonSerializer.Serialize(api, new JsonSerializerOptions { WriteIndented = true }));
Write(Path.Combine(outputDir, "library", "moontweaks.lua"), LuaCatsWriter.Write(api));

// A server writes the real one from its own registries. This empty one defines the
// alias so a checkout type-checks without a game to read codes out of.
Write(Path.Combine(outputDir, "library", MoonTweaks.Host.AssetCodeLibrary.FileName),
    MoonTweaks.Host.AssetCodeLibrary.Render([], []));
Write(Path.Combine(outputDir, "index.html"), HtmlWriter.Write(api));

Console.WriteLine($"{Members(api)} member(s) documented across "
                  + $"{api.Modules.Count} module(s), {api.Tables.Count} table(s), {api.Enums.Count} value set(s)");
return 0;

void Write(string path, string content)
{
    File.WriteAllText(path, content);
    Console.WriteLine($"  {Path.GetRelativePath(Environment.CurrentDirectory, path)}");
}

static int Members(ApiModel api) =>
    api.Modules.Sum(module => 1 + module.Functions.Count)
    + api.Tables.Sum(table => 1 + table.Fields.Count)
    + api.Enums.Sum(value => 1 + value.Values.Count);

/// <summary>
/// Every documented surface that carries no description. A non-empty result fails
/// the build, which is what keeps the reference honest as bindings are added.
/// </summary>
static IEnumerable<string> Undocumented(ApiModel api)
{
    foreach (var module in api.Modules)
    {
        if (Blank(module.Summary)) yield return $"module {module.Path}";
        foreach (var function in module.Functions)
        {
            if (Blank(function.Summary)) yield return $"function {module.Path}.{function.Name}";
            foreach (var parameter in function.Parameters.Where(p => Blank(p.Summary)))
            {
                yield return $"parameter {module.Path}.{function.Name}({parameter.Name})";
            }
        }
    }

    foreach (var table in api.Tables)
    {
        if (Blank(table.Summary)) yield return $"table {table.Name}";
        foreach (var field in table.Fields.Where(field => Blank(field.Summary)))
        {
            yield return $"field {table.Name}.{field.Name}";
        }
    }

    foreach (var enumeration in api.Enums)
    {
        if (Blank(enumeration.Summary)) yield return $"values {enumeration.Name}";
        foreach (var value in enumeration.Values.Where(value => Blank(value.Summary)))
        {
            yield return $"value {enumeration.Name}.{value.Name}";
        }
    }

    static bool Blank(string text) => string.IsNullOrWhiteSpace(text);
}
