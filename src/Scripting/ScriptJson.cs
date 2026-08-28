using System;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MoonTweaks.Scripting;

/// <summary>
/// The neutral value tree as JSON, and back. A utility rather than a system: it
/// reaches nothing and decides nothing, and the tree maps onto JSON exactly.
/// </summary>
/// <remarks>
/// Three unrelated things need it — the attributes a recipe carries, the attributes
/// an item carries, and whatever a script chooses to remember about a player — so it
/// belongs to none of them.
/// </remarks>
public static class ScriptJson
{
    /// <summary>
    /// One script value as the JSON token it already is. The tree maps onto JSON
    /// exactly, so nothing here decides anything: a list is an array, a table is an
    /// object, and the scalars are themselves.
    /// </summary>
    public static JToken Token(ScriptValue value) => value switch
    {
        ScriptValue.Str text => new JValue(text.Value),
        ScriptValue.Bool flag => new JValue(flag.Value),
        // Lua has one number type, so a whole number reaches here as a double and
        // would be written as one. The game turns these into typed attributes, and a
        // JSON recipe's 3 becomes a long there: writing 3.0 would hand it a double
        // instead, where a script and a recipe file saying the same thing should
        // leave the game holding the same thing.
        ScriptValue.Num number => new JValue(Whole(number.Value)),
        ScriptValue.List list => new JArray(list.Items.Select(Token).ToArray<object>()),
        ScriptValue.Map map => new JObject(
            map.Entries.Select(entry => new JProperty(entry.Key, Token(entry.Value))).ToArray<object>()),
        _ => JValue.CreateNull(),
    };

    /// <summary>
    /// One JSON token as the script value it came from. The other half of
    /// <see cref="Token"/>, so a value a script stored reads back as what it wrote.
    /// </summary>
    public static ScriptValue Read(JToken? token) => token switch
    {
        null => ScriptValue.Nil.Instance,
        JObject map => new ScriptValue.Map(
            map.Properties().ToDictionary(entry => entry.Name, entry => Read(entry.Value))),
        JArray list => new ScriptValue.List([.. list.Select(Read)]),
        JValue { Type: JTokenType.Null } => ScriptValue.Nil.Instance,
        JValue { Type: JTokenType.Boolean } flag => new ScriptValue.Bool(flag.Value<bool>()),
        JValue { Type: JTokenType.Integer or JTokenType.Float } number =>
            new ScriptValue.Num(number.Value<double>()),
        JValue value => new ScriptValue.Str(value.Value<string>() ?? ""),
        _ => ScriptValue.Nil.Instance,
    };

    /// <summary>A script value as the JSON text something else can store.</summary>
    public static string Write(ScriptValue value) => Token(value).ToString(Formatting.None);

    /// <summary>JSON text as the script value it holds, or nil when there is none.</summary>
    public static ScriptValue Parse(string? json) =>
        string.IsNullOrEmpty(json) ? ScriptValue.Nil.Instance : Read(JToken.Parse(json));

    /// <summary>A number as the integer it is, or as itself when it is not one.</summary>
    private static object Whole(double number) =>
        number % 1 == 0 && number is >= long.MinValue and <= long.MaxValue ? (long)number : number;
}
