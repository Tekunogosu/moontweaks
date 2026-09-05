using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MoonTweaks.Api;

/// <summary>
/// A mod that adds bindings of its own to every script MoonTweaks runs. Implemented
/// on one of the mod's <c>ModSystem</c> classes; MoonTweaks finds it through the
/// loader, so the mod registers nothing and needs no particular execute order beyond
/// declaring <c>moontweaks</c> as a dependency.
/// </summary>
/// <remarks>
/// Scripts reach a plugin's bindings under <c>plugin.&lt;Name&gt;</c>, and nowhere
/// else: every module a plugin binds must sit at that path or beneath it, and a path
/// already bound by MoonTweaks or by another plugin refuses the whole run rather than
/// being taken. A plugin is described to an editor the same way MoonTweaks describes
/// itself, by reflecting over its assembly at startup, so it ships the compiler's XML
/// documentation beside its DLL to have its summaries appear there.
/// </remarks>
public interface IMoonTweaksPlugin
{
    /// <summary>
    /// Name scripts reach the bindings under, as the last segment of
    /// <c>plugin.&lt;Name&gt;</c>. Lowercase letters, digits and underscores, starting
    /// with a letter, so that it is a Lua identifier.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// The objects carrying the bindings, each annotated with
    /// <see cref="LuaModuleAttribute"/>. Called once per run: a server's startup
    /// and every dry-run check each get fresh instances, so state one run leaves on
    /// them never reaches the next.
    /// </summary>
    /// <remarks>
    /// First called while assets are loaded, which the game does before it runs any
    /// mod's <c>StartServerSide</c>. A plugin therefore takes what its domains need
    /// in <c>Start</c>, and anything another mod only builds in its own server-side
    /// start is reached at call time rather than here.
    /// </remarks>
    IEnumerable<object> Domains();
}

/// <summary>
/// What a plugin and MoonTweaks agree on: the root every plugin binds under, and the
/// shape of a name. Sole owner of both, so the check at binding time and the path an
/// editor is told about cannot disagree.
/// </summary>
public static partial class PluginContract
{
    /// <summary>
    /// Version of this contract. Raised on a change a plugin built against the
    /// previous one would not survive, so a plugin can say which it was built for.
    /// </summary>
    public const int VERSION = 1;

    /// <summary>Table every plugin's bindings hang beneath.</summary>
    public const string ROOT = "plugin";

    /// <summary>The path scripts reach a plugin at.</summary>
    public static string PathOf(string name) => $"{ROOT}.{name}";

    /// <summary>Whether a name is one a plugin may carry.</summary>
    public static bool IsValidName(string? name) => name is not null && Name().IsMatch(name);

    /// <summary>Whether a module path is the plugin's own, or beneath it.</summary>
    public static bool Owns(string name, string path) =>
        path == PathOf(name) || path.StartsWith(PathOf(name) + ".", System.StringComparison.Ordinal);

    [GeneratedRegex("^[a-z][a-z0-9_]*$")]
    private static partial Regex Name();
}
