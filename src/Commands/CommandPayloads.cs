using MoonTweaks.Api;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;

namespace MoonTweaks.Commands;

/// <summary>
/// What a handler is told about the person who ran its command and what they typed.
/// Sole owner of that shape: the table a handler reads is written from these fields,
/// and so is the reference an editor completes against.
/// </summary>
/// <param name="caller">Who ran it.</param>
/// <param name="args">What they typed after the name, keyed by the names the command gave them.</param>
[LuaTable("CommandEvent", Given = true)]
public sealed class CommandPayload(Caller caller, ScriptValue args)
{
    /// <summary>
    /// Identifier of the player who ran it, which every <c>moontweaks.players</c>
    /// function takes. Nil when the server console ran it, which is what
    /// <c>requiresPlayer</c> rules out.
    /// </summary>
    [LuaField("player")]
    public string? Player { get; } = caller.Player?.PlayerUID;

    /// <summary>Name of whoever ran it, which is the console's own name when nobody did.</summary>
    [LuaField("playerName")]
    public string CallerName { get; } = caller.GetName();

    /// <summary>
    /// The values typed after the command's name, each under the name the command
    /// gave it. A value left out of an optional argument is absent here.
    /// </summary>
    [LuaField("args")]
    public ScriptValue Args { get; } = args;
}
