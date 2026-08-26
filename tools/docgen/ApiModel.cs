using System.Collections.Generic;

namespace MoonTweaks.DocGen;

/// <summary>The whole scriptable surface, as rendered into every documentation format.</summary>
public sealed record ApiModel(
    string Version,
    IReadOnlyList<ModuleDoc> Modules,
    IReadOnlyList<TableDoc> Tables,
    IReadOnlyList<EnumDoc> Enums);

/// <summary>A dotted path scripts call functions on.</summary>
public sealed record ModuleDoc(string Path, string Summary, IReadOnlyList<FunctionDoc> Functions);

/// <summary>One callable function.</summary>
public sealed record FunctionDoc(
    string Name,
    string Summary,
    IReadOnlyList<ParameterDoc> Parameters,
    string Returns);

/// <summary>One argument a script supplies.</summary>
public sealed record ParameterDoc(string Name, string Type, string Summary);

/// <summary>A table shape scripts write as a literal.</summary>
public sealed record TableDoc(
    string Name,
    string Summary,
    string? Shorthand,
    IReadOnlyList<FieldDoc> Fields);

/// <summary>One key of a table shape.</summary>
public sealed record FieldDoc(
    string Name,
    string Type,
    bool Required,
    string? Default,
    string Summary);

/// <summary>A closed set of string values a field accepts.</summary>
public sealed record EnumDoc(string Name, string Summary, IReadOnlyList<EnumValueDoc> Values);

/// <summary>One accepted value of an enumeration.</summary>
public sealed record EnumValueDoc(string Name, string Summary);
