using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MoonTweaks.Api;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;

namespace MoonTweaks.Host;

/// <summary>
/// A plugin that cannot be taken as it is. Nothing about one is recoverable by
/// MoonTweaks: the name, the paths and the bindings are the plugin author's, so the
/// run refuses and says which plugin and why, for the operator to take up with them.
/// </summary>
public sealed class PluginError(string message) : Exception(message);

/// <summary>One plugin the loader holds, with the mod it came from.</summary>
/// <param name="Name">Name scripts reach it under, already checked.</param>
/// <param name="Source">The mod system implementing the contract.</param>
/// <param name="Mod">The mod that system belongs to, for naming it.</param>
public sealed record Plugin(string Name, IMoonTweaksPlugin Source, Mod Mod)
{
    /// <summary>Path scripts reach the plugin at.</summary>
    public string Path => PluginContract.PathOf(Name);

    /// <summary>File its type library is written to, beside MoonTweaks's own.</summary>
    public string LibraryFile => $"{Path}.lua";

    /// <summary>The assembly its bindings are declared in.</summary>
    public Assembly Assembly => Source.GetType().Assembly;

    /// <summary>The plugin as a failure or a report names it.</summary>
    public string Describe() =>
        $"plugin '{Name}' from mod {Mod.Info?.ModID ?? "?"} {Mod.Info?.Version ?? ""}".TrimEnd();
}

/// <summary>A plugin whose domains a run bound, with the paths scripts reach them at.</summary>
public sealed record BoundPlugin(Plugin Plugin, IReadOnlyList<string> Paths);

/// <summary>
/// Finding the plugins a server holds and hanging their bindings on a run. Sole
/// owner of the contract's checks, so a plugin refused at startup is refused the
/// same way by a dry-run check.
/// </summary>
public static class Plugins
{
    /// <summary>
    /// Every plugin the loader holds, in name order. Refuses a name that is not a
    /// plugin name and two plugins claiming one name, since either would leave
    /// scripts reaching a path nobody can say the owner of.
    /// </summary>
    public static IReadOnlyList<Plugin> Discover(IModLoader loader)
    {
        var byName = new Dictionary<string, Plugin>(StringComparer.Ordinal);

        foreach (var system in loader.Systems)
        {
            if (system is not IMoonTweaksPlugin source) continue;

            var plugin = new Plugin(source.Name ?? "", source, system.Mod);

            if (!PluginContract.IsValidName(source.Name))
            {
                throw new PluginError(
                    $"{plugin.Describe()} calls itself '{source.Name}', which is not a plugin name: "
                    + "lowercase letters, digits and underscores, starting with a letter");
            }

            if (byName.TryGetValue(plugin.Name, out var other))
            {
                throw new PluginError($"{plugin.Describe()} and {other.Describe()} both claim the name '{plugin.Name}'");
            }

            byName[plugin.Name] = plugin;
        }

        return byName.Values.OrderBy(plugin => plugin.Name, StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// Binds one plugin's domains, each at a path the plugin owns and nobody else has
    /// taken. A path outside <c>plugin.&lt;name&gt;</c> or already bound refuses the
    /// plugin outright: MoonTweaks's own namespace stays its own, and a script
    /// reaching a path always reaches the one plugin that declared it.
    /// </summary>
    /// <param name="plugin">The plugin to bind.</param>
    /// <param name="taken">Every path bound so far, which this adds to.</param>
    /// <param name="bind">Where an accepted module goes.</param>
    public static BoundPlugin Bind(Plugin plugin, ISet<string> taken, Action<ModuleBinding> bind)
    {
        var paths = new List<string>();

        foreach (var module in Modules(plugin))
        {
            if (!PluginContract.Owns(plugin.Name, module.Path))
            {
                throw new PluginError(
                    $"{plugin.Describe()} binds '{module.Path}', which is outside its own '{plugin.Path}'");
            }

            if (!taken.Add(module.Path))
            {
                throw new PluginError($"{plugin.Describe()} binds '{module.Path}', which is already bound");
            }

            bind(module);
            paths.Add(module.Path);
        }

        return new BoundPlugin(plugin, paths);
    }

    /// <summary>
    /// What one plugin binds, reduced to modules. A plugin that throws building its
    /// domains, or hands over an object with no module annotation, is refused with
    /// its own name on the failure rather than a stack trace out of the run.
    /// </summary>
    private static IReadOnlyList<ModuleBinding> Modules(Plugin plugin)
    {
        try
        {
            return plugin.Source.Domains().Select(DomainBinder.Bind).ToList();
        }
        catch (Exception failure) when (failure is not PluginError)
        {
            throw new PluginError($"{plugin.Describe()} failed building its bindings: {failure.Message}");
        }
    }
}
