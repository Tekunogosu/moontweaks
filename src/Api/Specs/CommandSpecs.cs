using MoonTweaks.Scripting;

namespace MoonTweaks.Api;

// What a script writes to give players something to type.

/// <summary>What kind of value a command reads from what was typed after its name.</summary>
public enum ArgumentKind
{
    /// <summary>One word.</summary>
    Word,

    /// <summary>A whole number.</summary>
    Int,

    /// <summary>A number, whole or not.</summary>
    Number,

    /// <summary>On or off.</summary>
    Bool,

    /// <summary>Everything left on the line, spaces included.</summary>
    Text,

    /// <summary>A player who is online, which a handler is given the identifier of.</summary>
    Player,
}

/// <summary>One value a command takes after its name.</summary>
[LuaTable("CommandArgument")]
public sealed class CommandArgumentSpec
{
    /// <summary>
    /// Names the value, both in the syntax the game prints and as the key a handler
    /// reads it under.
    /// </summary>
    [LuaField("name", Required = true)]
    public string Name { get; set; } = "";

    /// <summary>What kind of value it is, which decides how the game reads it.</summary>
    [LuaField("type", Required = true)]
    public ArgumentKind Type { get; set; }

    /// <summary>
    /// Whether the command still runs when this is left out. An omitted value reaches
    /// the handler as nothing, so a handler taking one has to expect that.
    /// </summary>
    [LuaField("optional", Default = "false")]
    public bool Optional { get; set; }

    /// <summary>
    /// The only values accepted, which the game also offers as completions. Bound on
    /// <c>word</c> alone, where the game has a parser for it.
    /// </summary>
    [LuaField("values")]
    public string[]? Values { get; set; }
}

/// <summary>
/// A command players may type, or one step of one. A command either does something
/// itself or gathers others under it; the game refuses one that does neither.
/// </summary>
[LuaTable("Command")]
public sealed class CommandSpec
{
    /// <summary>
    /// Word players type. On the outermost command this follows the slash, so
    /// <c>myrequest</c> is typed <c>/myrequest</c>; below one it is the next word.
    /// </summary>
    [LuaField("name", Required = true)]
    public string Name { get; set; } = "";

    /// <summary>
    /// What it does, which the game prints in <c>/help</c> and in its own syntax
    /// errors. Required, on this and on every command beneath it.
    /// </summary>
    [LuaField("description", Required = true)]
    public string Description { get; set; } = "";

    /// <summary>
    /// Privilege a caller must hold. Inherited by everything beneath it, so it is
    /// usually set once on the outermost command. Defaults to the one every player
    /// who may talk already holds; name <c>controlserver</c> for an administrators'
    /// command.
    /// </summary>
    [LuaField("privilege", Default = "\"chat\"")]
    public string Privilege { get; set; } = "chat";

    /// <summary>
    /// Whether a real player must have typed it. Left alone, the server console may
    /// run it too, and a handler is given no player when it does.
    /// </summary>
    [LuaField("requiresPlayer", Default = "false")]
    public bool RequiresPlayer { get; set; }

    /// <summary>Values typed after the name, in the order they are typed.</summary>
    [LuaField("args")]
    public CommandArgumentSpec[]? Args { get; set; }

    /// <summary>
    /// Called when someone runs it. Required unless <c>subcommands</c> names what to
    /// run instead.
    /// </summary>
    [LuaField("handler")]
    [LuaPayload(typeof(MoonTweaks.Commands.CommandPayload), Returns = "string|table|nil")]
    public ScriptValue.Func? Handler { get; set; }

    /// <summary>
    /// Commands gathered under this one, each typed as a further word. A command with
    /// these needs no handler of its own.
    /// </summary>
    [LuaField("subcommands")]
    public CommandSpec[]? Subcommands { get; set; }
}
