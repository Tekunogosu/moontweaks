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
    /// <summary>What this server's scripts applied at startup, for the list command.</summary>
    private MutationLog? applied;

    /// <summary>The vanilla recipe loader runs at 1.0; scripts must observe its output.</summary>
    public override double ExecuteOrder() => 1.1;

    /// <summary>Recipes are server-authoritative, so only the server runs scripts.</summary>
    public override bool ShouldLoad(EnumAppSide side) => side == EnumAppSide.Server;

    /// <summary>
    /// Registers the command surface. Its privilege comes from the settings file, so
    /// a server that has not chosen one keeps commands with the administrators.
    /// </summary>
    public override void StartServerSide(ICoreServerAPI api)
    {
        var folder = ScriptLibrary.PathFor();
        var config = MoonTweaksConfig.Load(folder, api.Logger);

        api.ChatCommands.Create("moontweaks")
            .WithDescription("MoonTweaks scripting tools")
            .RequiresPrivilege(config.CommandPrivilege)
            .BeginSubCommand("list")
                .WithDescription("List the changes this server's scripts applied at startup")
                .HandleWith(_ =>
                {
                    if (applied is not { Changes.Count: > 0 } log)
                    {
                        return TextCommandResult.Success("no scripts have changed anything on this server");
                    }

                    return TextCommandResult.Success(
                        $"{log.Changes.Count} change(s) applied at startup:\n"
                        + string.Join("\n", log.Changes));
                })
            .EndSubCommand()
            .BeginSubCommand("check")
                .WithDescription("Re-run every script and report what it would change, changing nothing")
                .HandleWith(_ =>
                {
                    var run = ScriptRun.Execute(api, ScriptLibrary.ScriptsPathFor(), new RecipeRegistry(api));

                    if (run.Failure is { } failure) return TextCommandResult.Error(failure.Message);
                    if (run.Scripts.Count == 0) return TextCommandResult.Success("no scripts to check");

                    var lines = string.Join("\n", run.Describe());
                    return TextCommandResult.Success(
                        $"{run.Scripts.Count} script(s) ran, {run.Log.Pending.Count} change(s) would be made"
                        + (lines.Length == 0 ? "" : "\n" + lines)
                        + "\nnothing was applied; restart the server for changes to take effect");
                })
            .EndSubCommand()
            .BeginSubCommand("export")
                .WithDescription("Rewrite the asset codes an editor suggests, from the live registries")
                .HandleWith(_ =>
                {
                    var codes = AssetCodeLibrary.Install(folder, api.World, force: true);
                    return TextCommandResult.Success(
                        $"wrote {EditorSupport.LibraryFolder}/{AssetCodeLibrary.FileName} with {codes} asset code(s)");
                })
            .EndSubCommand();
    }

    /// <inheritdoc/>
    public override void AssetsLoaded(ICoreAPI api)
    {
        if (api is not ICoreServerAPI server) return;

        var folder = ScriptLibrary.PathFor();
        var scriptsFolder = ScriptLibrary.ScriptsPathFor();
        EditorSupport.Install(folder, server.Logger);

        // The registries are populated by now, so the codes an author may write are
        // exactly the ones an editor can offer.
        if (AssetCodeLibrary.Install(folder, server.World) is { } codes)
        {
            server.Logger.Notification("[moontweaks] wrote {0}/{1} with {2} asset code(s)",
                EditorSupport.LibraryFolder, AssetCodeLibrary.FileName, codes);
        }

        foreach (var misplaced in ScriptLibrary.Misplaced(folder))
        {
            server.Logger.Warning("[moontweaks] {0} sits beside the {1} folder rather than in it, so it does not run",
                misplaced, ScriptLibrary.ScriptsFolder);
        }

        var registry = new RecipeRegistry(server);
        var run = ScriptRun.Execute(server, scriptsFolder, registry);

        if (run.Scripts.Count == 0)
        {
            server.Logger.Notification("[moontweaks] no scripts in {0}", scriptsFolder);
            return;
        }

        if (run.Failure is { } failure)
        {
            // Nothing has been applied yet, so abandoning the run leaves the
            // registries exactly as the vanilla loader left them.
            server.Logger.Error("[moontweaks] {0}", failure.Message);
            server.Logger.Error("[moontweaks] no changes were applied");
            return;
        }

        var affected = run.Log.Apply(server, server.Logger);
        applied = run.Log;
        RecipeBase.CollectiblePreSearchResultsCache.Clear();

        server.Logger.Notification(
            "[moontweaks] {0} script(s), {1} change(s), {2} recipe(s) affected; "
            + "{3} grid and {4} knapping recipes now",
            run.Scripts.Count, run.Log.Pending.Count, affected,
            server.World.GridRecipes.Count, registry.Knapping.Count);
    }
}
