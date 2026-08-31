using System.Collections.Generic;

namespace MoonTweaks.DocGen;

/// <summary>The whole scriptable surface, as rendered into every documentation format.</summary>
public sealed record ApiModel(
    string Version,
    IReadOnlyList<ModuleDoc> Modules,
    IReadOnlyList<TableDoc> Tables,
    IReadOnlyList<EnumDoc> Enums);

/// <summary>A dotted path scripts call functions on.</summary>
/// <param name="Path">What a script writes to reach it.</param>
/// <param name="Summary">What the module is for.</param>
/// <param name="Example">
/// A few lines of Lua showing the module in use. Written beside the binding as an
/// XML <c>example</c> element, so the reference, the editor library and the checked
/// snippets are all one source.
/// </param>
/// <param name="Functions">What may be called on it.</param>
public sealed record ModuleDoc(
    string Path,
    string Summary,
    string Example,
    IReadOnlyList<FunctionDoc> Functions);

/// <summary>One callable function.</summary>
public sealed record FunctionDoc(
    string Name,
    string Summary,
    IReadOnlyList<ParameterDoc> Parameters,
    string Returns);

/// <summary>One argument a script supplies.</summary>
public sealed record ParameterDoc(string Name, string Type, string Summary);

/// <summary>A table shape, whether scripts write it as a literal or are handed it.</summary>
/// <param name="Name">Name the shape is documented under.</param>
/// <param name="Summary">What the shape describes.</param>
/// <param name="Shorthand">Field a bare string stands in for, when the shape has one.</param>
/// <param name="Given">
/// Whether the shape is handed to a script rather than written by one, as an event's
/// table is. A given shape has no defaults: every key is filled in before a handler
/// sees it, and <see cref="FieldDoc.Required"/> says which of them may still be nil.
/// </param>
/// <param name="Fields">The shape's keys.</param>
public sealed record TableDoc(
    string Name,
    string Summary,
    string? Shorthand,
    bool Given,
    IReadOnlyList<FieldDoc> Fields);

/// <summary>One key of a table shape.</summary>
/// <param name="Name">Key as scripts write or read it.</param>
/// <param name="Type">Type the key holds.</param>
/// <param name="Required">
/// Whether the key is always there: on a shape scripts write, that omitting it is an
/// error; on one they are given, that it is never nil.
/// </param>
/// <param name="Default">Value used when the key is omitted, on a shape scripts write.</param>
/// <param name="Summary">What the key means.</param>
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
