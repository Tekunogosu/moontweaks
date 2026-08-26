using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace MoonTweaks.Host;

/// <summary>
/// Reports what the client received, so a test can check it against what the
/// server reported. Read-only on purpose: it must not influence the result.
/// </summary>
public class ClientSyncProbe : ModSystem
{
    /// <summary>Only the client has a received registry worth measuring.</summary>
    public override bool ShouldLoad(EnumAppSide side) => side == EnumAppSide.Client;

    /// <inheritdoc/>
    public override void StartClientSide(ICoreClientAPI api)
    {
        api.Event.LevelFinalize += () =>
            api.Logger.Notification("[moontweaks probe] client sees {0} grid recipes",
                api.World.GridRecipes.Count);
    }
}
