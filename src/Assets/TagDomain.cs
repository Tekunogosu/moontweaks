using MoonTweaks.Api;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;

namespace MoonTweaks.Assets;

/// <summary>
/// The tag names this server knows, and declaring new ones.
/// </summary>
/// <remarks>
/// A tag says what something is rather than what it is called, so one rule reaches a
/// modded axe as readily as a vanilla one. The game ships a set of them
/// and a script may declare its own, then put them on items and blocks through
/// <c>addTags</c> on <c>moontweaks.items.set</c> and <c>moontweaks.blocks.set</c>.
/// Every place that already selects by tags — an asset change, a recipe ingredient, a
/// recipe selector — reads a declared tag exactly as it reads one of the game's.
///
/// Declaring belongs in a script's body. The server closes its tag registry as soon
/// as the scripts have run, so a handler or a timer is too late and is told so.
///
/// Players need nothing installed. The server sends its whole tag table to every
/// client as they connect, so a tag declared here is one their game knows too.
///
/// This is the tags an item or block carries. Creatures carry tags from a registry of
/// their own, which is unbound: nothing has asked for it.
/// </remarks>
/// <example>
/// <code>
/// local tags = moontweaks.tags
///
/// -- Declared once, in a script's body, before anything carries them.
/// tags.add { "mymod:scrap-metal", "mymod:ritual-component" }
/// tags.add "mymod:contraband"
///
/// -- Then put on whatever should carry them, and selected by afterwards.
/// moontweaks.items.set {
///   code = "game:metalbit-*",
///   addTags = "mymod:scrap-metal",
/// }
///
/// moontweaks.items.set {
///   tags = { "mymod:scrap-metal" },
///   maxStackSize = 128,
/// }
/// </code>
/// </example>
[LuaModule("moontweaks.tags")]
public sealed class TagDomain(IWorldAccessor world)
{
    /// <summary>
    /// Declares tag names for this server run, so assets may carry them and conditions
    /// may ask for them. A name the server already knows is left alone, so two scripts
    /// declaring the same tag is not a clash.
    /// </summary>
    /// <remarks>
    /// One name may be written on its own; several are written as a list. The
    /// declaration lasts as long as the server runs and is made again at every
    /// startup, which is why it belongs in a script's body.
    /// </remarks>
    /// <param name="origin">Script line declaring them.</param>
    /// <param name="names">Name to declare, or a list of them.</param>
    [LuaFunction("add")]
    public void Add(ScriptOrigin origin, string[] names) =>
        TagRegistration.Declare(world.Api.CollectibleTagRegistry, names, origin);
}
