using MoonTweaks.Api;
using MoonTweaks.Recipes;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace MoonTweaks.Host;

/// <summary>
/// Runs the server's Lua scripts once the vanilla recipe loader has finished, then
/// applies everything they asked for in a single pass.
/// </summary>
public class MoonTweaksSystem : ModSystem
{
    /// <summary>The vanilla recipe loader runs at 1.0; scripts must observe its output.</summary>
    public override double ExecuteOrder() => 1.1;

    /// <summary>Recipes are server-authoritative, so only the server runs scripts.</summary>
    public override bool ShouldLoad(EnumAppSide side) => side == EnumAppSide.Server;

    /// <inheritdoc/>
    public override void AssetsLoaded(ICoreAPI api)
    {
        if (api is not ICoreServerAPI server) return;

        var log = new MutationLog();
        using var host = new MoonSharpHost();
        host.Bind(DomainBinder.Bind(new GridDomain(log, server.World)));
        host.Bind(DomainBinder.Bind(new LogDomain(server.Logger)));

        var scripts = ScriptLibrary.Discover(server);
        if (scripts.Count == 0)
        {
            server.Logger.Notification("[moontweaks] no scripts in {0}", ScriptLibrary.PathFor(server));
            return;
        }

        foreach (var script in scripts)
        {
            try
            {
                host.Run(script);
            }
            catch (ScriptError error)
            {
                // Nothing has been applied yet, so abandoning the run leaves the
                // registries exactly as the vanilla loader left them.
                server.Logger.Error("[moontweaks] {0}", error.Message);
                server.Logger.Error("[moontweaks] no changes were applied");
                return;
            }
        }

        var affected = log.Apply(server, server.Logger);
        RecipeBase.CollectiblePreSearchResultsCache.Clear();

        server.Logger.Notification(
            "[moontweaks] {0} script(s), {1} change(s), {2} recipe(s) affected; {3} grid recipes now",
            scripts.Count, log.Pending.Count, affected, server.World.GridRecipes.Count);
    }
}
