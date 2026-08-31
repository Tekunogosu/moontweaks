using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace MoonTweaks.DocGen;

/// <summary>
/// The compiler's XML documentation output, indexed by member. Descriptions come
/// from here rather than from attributes so that the doc comment a developer writes
/// beside the code is the one that reaches the reference.
/// </summary>
public sealed partial class XmlDocs
{
    private readonly Dictionary<string, XElement> members;

    private XmlDocs(Dictionary<string, XElement> members) => this.members = members;

    /// <summary>Loads the XML file the compiler emitted next to an assembly.</summary>
    public static XmlDocs Load(string path) => new(
        XDocument.Load(path)
            .Descendants("member")
            .Where(member => member.Attribute("name") is not null)
            .GroupBy(member => member.Attribute("name")!.Value)
            .ToDictionary(group => group.Key, group => group.First()));

    /// <summary>Summary of a type, or the empty string when it has none.</summary>
    public string Summary(Type type) => SectionOf($"T:{FullNameOf(type)}", "summary");

    /// <summary>
    /// The worked example on a type, kept line for line, or the empty string when it
    /// has none. Unlike a summary, an example is code: the whitespace inside it is
    /// what makes it readable, so it is dedented rather than flattened.
    /// </summary>
    public string Example(Type type) =>
        members.TryGetValue($"T:{FullNameOf(type)}", out var member)
            ? Dedent(member.Element("example"))
            : "";

    /// <summary>Summary of a property.</summary>
    public string Summary(PropertyInfo property) =>
        SectionOf($"P:{FullNameOf(property.DeclaringType!)}.{property.Name}", "summary");

    /// <summary>Summary of an enumeration value.</summary>
    public string Summary(Type enumType, string value) =>
        SectionOf($"F:{FullNameOf(enumType)}.{value}", "summary");

    /// <summary>Summary of a method.</summary>
    public string Summary(MethodInfo method) => SectionOf(KeyOf(method), "summary");

    /// <summary>Description of one named parameter of a method.</summary>
    public string Parameter(MethodInfo method, string name) =>
        members.TryGetValue(KeyOf(method), out var member)
            ? Flatten(member.Elements("param")
                .FirstOrDefault(element => element.Attribute("name")?.Value == name))
            : "";

    /// <summary>The XML documentation key for a method, including its parameter types.</summary>
    private static string KeyOf(MethodInfo method)
    {
        var parameters = method.GetParameters();
        var signature = parameters.Length == 0
            ? ""
            : "(" + string.Join(",", parameters.Select(parameter => FullNameOf(parameter.ParameterType))) + ")";
        return $"M:{FullNameOf(method.DeclaringType!)}.{method.Name}{signature}";
    }

    /// <summary>Type name as the documentation format spells it.</summary>
    private static string FullNameOf(Type type) =>
        (type.FullName ?? type.Name).Replace('+', '.').Split('`')[0] is var name && type.IsGenericType
            ? name + "{" + string.Join(",", type.GetGenericArguments().Select(FullNameOf)) + "}"
            : (type.FullName ?? type.Name).Replace('+', '.');

    /// <summary>
    /// An element's text with the indentation the doc comment added taken back off,
    /// measured from the least indented line so the shape inside it survives.
    /// </summary>
    private static string Dedent(XElement? element)
    {
        if (element is null) return "";

        var lines = element.Value.Replace("\r\n", "\n").Split('\n').ToList();
        while (lines.Count > 0 && lines[0].Trim().Length == 0) lines.RemoveAt(0);
        while (lines.Count > 0 && lines[^1].Trim().Length == 0) lines.RemoveAt(lines.Count - 1);
        if (lines.Count == 0) return "";

        var indent = lines.Where(line => line.Trim().Length > 0)
            .Min(line => line.Length - line.TrimStart().Length);

        return string.Join("\n", lines.Select(line => line.Length >= indent ? line[indent..] : line.TrimStart()));
    }

    private string SectionOf(string key, string section) =>
        members.TryGetValue(key, out var member) ? Flatten(member.Element(section)) : "";

    /// <summary>Collapses a documentation element to plain text, keeping code spans marked.</summary>
    private static string Flatten(XElement? element)
    {
        if (element is null) return "";

        var text = string.Concat(element.Nodes().Select(node => node switch
        {
            XText raw => raw.Value,
            XElement { Name.LocalName: "c" } code => $"`{code.Value}`",
            XElement { Name.LocalName: "see" } reference =>
                $"`{reference.Attribute("cref")?.Value.Split('.').Last() ?? ""}`",
            XElement other => other.Value,
            _ => "",
        }));

        return Whitespace().Replace(text, " ").Trim();
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
