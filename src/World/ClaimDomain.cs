using System.Collections.Generic;
using MoonTweaks.Api;
using MoonTweaks.Scripting;

namespace MoonTweaks.World;

/// <summary>
/// The land people have claimed, and the claims a script makes on their behalf.
/// </summary>
/// <remarks>
/// A claim is the game's own protection rather than one this mod invents, which is
/// what makes it worth reaching: the server saves it with the world and sends it to
/// every client itself, so land a script claims is protected and drawn for players
/// who have installed nothing.
///
/// This is one of three things a script has for protection, and they answer different
/// questions. <c>moontweaks.world.testAccess</c> asks whether one player may act
/// somewhere and is the whole answer — it runs the claim check and then every other
/// mod's, so it already accounts for protections this mod knows nothing about. This
/// module reads and writes the claims themselves. <c>moontweaks.reinforce</c> reaches
/// the separate protection the survival mod puts on a single block.
/// </remarks>
/// <example>
/// <code>
/// local claims = moontweaks.claims
///
/// -- What is claimed where somebody is standing, and by whom.
/// for _, claim in ipairs(claims.at(500, 110, 500)) do
///   moontweaks.log.info(("%s holds claim %d here: %s")
///     :format(claim.ownerName, claim.index, claim.description))
/// end
///
/// -- Claiming a plot for somebody, and reading back the number it was given.
/// local number = claims.add {
///   owner = somebodyUid,
///   x = 480, y = 0,   z = 480,
///   toX = 520, toY = 256, toZ = 520,
///   description = "Riverside plot",
/// }
///
/// claims.remove(somebodyUid, number)
/// </code>
/// </example>
[LuaModule("moontweaks.claims")]
public sealed class ClaimDomain(ClaimAccess claims)
{
    /// <summary>
    /// Every claim covering a block, which is usually none and may be more than one
    /// where claims overlap.
    /// </summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="x">Which block, east to west.</param>
    /// <param name="y">Which block, from the world's floor upwards.</param>
    /// <param name="z">Which block, north to south.</param>
    [LuaFunction("at")]
    public IReadOnlyList<ClaimPayload> At(ScriptOrigin origin, int x, int y, int z) =>
        claims.At(x, y, z);

    /// <summary>
    /// One player's claims, in the order the game numbers them — which is the order
    /// <c>/land list</c> shows that player, so a number here is a number they can be
    /// told.
    /// </summary>
    /// <remarks>
    /// Answers for somebody who is not connected, which is most of what this is for:
    /// tidying up after a player who has left needs their claims without needing them.
    /// </remarks>
    /// <param name="origin">Script line asking.</param>
    /// <param name="player">Identifier of the owner, as an event gives it.</param>
    [LuaFunction("of")]
    public IReadOnlyList<ClaimPayload> Of(ScriptOrigin origin, string player) =>
        claims.Of(player, origin);

    /// <summary>
    /// Claims a box of land for a player, and answers with the number the new claim
    /// was given among theirs. Saved with the world and drawn for every client by the
    /// game itself.
    /// </summary>
    /// <remarks>
    /// Nothing here checks what the game checks when a player claims land themselves:
    /// how much they are allowed, how many separate claims they may hold, or whether
    /// the box overlaps somebody else's. A script claiming land is the server acting
    /// rather than a player asking, the same way <c>world.setBlock</c> builds without
    /// asking a claim. A script handing land out on request should read
    /// <c>claims.at</c> first and decide for itself.
    /// </remarks>
    /// <param name="origin">Script line making the claim.</param>
    /// <param name="claim">Who it is for, which land, and how it is held.</param>
    [LuaFunction("add")]
    public int Add(ScriptOrigin origin, ClaimSpec claim) => claims.Add(claim, origin);

    /// <summary>
    /// Takes back one of a player's claims, named by its number, and says whether
    /// there was one to take.
    /// </summary>
    /// <remarks>
    /// The number is a position rather than a name, so taking one back moves every
    /// later claim of that owner down one. A script removing several should read the
    /// numbers again between removals, or work downwards from the highest.
    /// </remarks>
    /// <param name="origin">Script line removing it.</param>
    /// <param name="player">Identifier of the owner, as an event gives it.</param>
    /// <param name="index">Its number among their claims, counting from zero.</param>
    [LuaFunction("remove")]
    public bool Remove(ScriptOrigin origin, string player, int index) =>
        claims.Remove(player, index, origin);
}
