using MoonTweaks.Api;
using MoonTweaks.Scripting;

namespace MoonTweaks.Commands;

/// <summary>
/// Commands players may type. A script declares one and is called back whenever
/// somebody runs it.
/// </summary>
/// <remarks>
/// Unlike a recipe, a command needs nothing installed on a player's machine: the
/// client sends the line as typed and the server reads it, so a command a script
/// declares works for everyone already connected to that server. Like a recipe, it
/// is declared as the server loads, so a new one still wants a restart.
/// </remarks>
/// <example>
/// <code>
/// moontweaks.commands.add {
///   name = "home",
///   description = "Say where you are standing",
///   privilege = "chat",
///   requiresPlayer = true,
///
///   handler = function(e)
///     local at = moontweaks.players.position(e.player)
///     return ("%s, you are at %.0f %.0f %.0f"):format(e.playerName, at.x, at.y, at.z)
///   end,
/// }
/// </code>
/// </example>
[LuaModule("moontweaks.commands")]
public sealed class CommandDomain(ScriptCommands commands)
{
    /// <summary>
    /// Declares a command. Its name is refused if the server already has one, since
    /// the game allows a name to be taken only once.
    /// </summary>
    /// <param name="origin">Script line declaring it.</param>
    /// <param name="command">The command to declare.</param>
    [LuaFunction("add")]
    public void Add(ScriptOrigin origin, CommandSpec command) => commands.Declare(command, origin);
}
