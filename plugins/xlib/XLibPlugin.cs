using System.Collections.Generic;
using MoonTweaks.Api;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace MoonTweaks.Plugins.XLib;

/// <summary>
/// The mod system MoonTweaks finds this plugin through. It registers nothing: being
/// a loaded mod system that implements the contract is what makes it a plugin, and
/// depending on <c>moontweaks</c> in the mod info is what has it loaded in time.
/// </summary>
public sealed class XLibPlugin : ModSystem, IMoonTweaksPlugin
{
    private ICoreServerAPI? server;

    /// <summary>Scripts reach the bindings as <c>plugin.xlib</c>.</summary>
    public string Name => "xlib";

    /// <summary>Scripts run on the server, so that is the only side with anything to bind.</summary>
    public override bool ShouldLoad(EnumAppSide side) => side == EnumAppSide.Server;

    /// <summary>
    /// The API is taken here rather than in <c>StartServerSide</c>: MoonTweaks runs
    /// scripts while assets are loaded, which the game does before it runs any mod's
    /// server-side start, so this is the one start every plugin has already had.
    /// </summary>
    public override void Start(ICoreAPI api) => server = api as ICoreServerAPI;

    /// <summary>A fresh domain per run, as the contract asks.</summary>
    public IEnumerable<object> Domains()
    {
        yield return new SkillsDomain(server!);
    }
}
