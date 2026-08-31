using MoonTweaks.Api;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;

namespace MoonTweaks.Host;

/// <summary>Messages from scripts into the server log.</summary>
/// <example>
/// <code>
/// local log = moontweaks.log
///
/// log.info(("starting with %d grid recipe(s)"):format(moontweaks.recipes.grid.count()))
///
/// if not moontweaks.mods.isEnabled("primitivesurvival") then
///   log.warn("primitive survival is not here, so its recipes were left alone")
/// end
/// </code>
/// </example>
[LuaModule("moontweaks.log")]
public sealed class LogDomain(ILogger logger)
{
    /// <summary>Writes an informational message, prefixed with the script and line.</summary>
    /// <param name="origin">Script line requesting the message.</param>
    /// <param name="message">Text to write.</param>
    [LuaFunction("info")]
    public void Info(ScriptOrigin origin, string message) =>
        logger.Notification("[moontweaks] {0}: {1}", origin, message);

    /// <summary>Writes a warning, prefixed with the script and line.</summary>
    /// <param name="origin">Script line requesting the message.</param>
    /// <param name="message">Text to write.</param>
    [LuaFunction("warn")]
    public void Warn(ScriptOrigin origin, string message) =>
        logger.Warning("[moontweaks] {0}: {1}", origin, message);
}
