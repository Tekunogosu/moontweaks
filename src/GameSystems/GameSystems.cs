using MoonTweaks.Scripting;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace MoonTweaks.GameSystems;

/// <summary>
/// Reaching a system another mod declared. Sole owner of that step, so every domain
/// built on one asks the same way and a server missing the mod is told the same
/// thing whichever of them was called.
/// </summary>
/// <remarks>
/// Everything reached this way is another mod's internals rather than the game's
/// API. The vanilla API is versioned and deprecates before it removes; a mod system
/// is neither, so a survival update may rename a method under this and the first
/// anybody hears of it is a build that fails or a server that reports a system it
/// cannot find. <c>MODSYSTEMS.md</c> lists every member reached this way, and is the
/// list to walk after a game update.
///
/// The types are referenced rather than reflected over, so a rename fails the build
/// here instead of failing a script on somebody's server. That is only affordable
/// because the coupling already exists: the recipe kinds are the survival mod's own
/// types and this mod has never run without it.
/// </remarks>
public sealed class GameSystems(ICoreServerAPI api)
{
    /// <summary>
    /// One mod system, or a failure naming the mod a server would have to install to
    /// get it. Never null, so a domain built on one reads as though it were always
    /// there.
    /// </summary>
    /// <param name="mod">Identifier of the mod declaring it, as a failure should name it.</param>
    /// <param name="what">What the script was trying to do, as a failure should read it.</param>
    /// <param name="origin">Script line asking.</param>
    public TSystem Required<TSystem>(string mod, string what, ScriptOrigin origin)
        where TSystem : ModSystem =>
        api.ModLoader.GetModSystem<TSystem>()
        ?? throw new ScriptError(origin,
            $"{what} needs the '{mod}' mod, which this server does not have loaded");

    /// <summary>Whether a system is there at all, for a script that would rather ask than fail.</summary>
    public bool Has<TSystem>() where TSystem : ModSystem =>
        api.ModLoader.GetModSystem<TSystem>() is not null;
}
