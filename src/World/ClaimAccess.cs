using System.Collections.Generic;
using System.Linq;
using MoonTweaks.Api;
using MoonTweaks.Players;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace MoonTweaks.World;

/// <summary>
/// Reaching the land claims a world holds. Sole owner of how a script names one:
/// by its owner and its number among that owner's claims, which is the numbering the
/// game's own <c>/land</c> commands use and show.
/// </summary>
/// <remarks>
/// The game keeps no identifier on a claim — <c>LandClaim</c> is twelve fields of
/// data and none of them is a name — and removes one by object rather than by
/// anything a script could hold. So the object never leaves this class: what crosses
/// into a script is the pair the game itself indexes by, and what comes back is
/// resolved through the same ordered list <c>/land free</c> resolves through.
/// </remarks>
/// <param name="api">The running server.</param>
/// <param name="players">
/// How an identifier becomes a player. Borrowed rather than repeated, so an
/// identifier naming nobody reads the same here as everywhere else.
/// </param>
public sealed class ClaimAccess(ICoreServerAPI api, PlayerAccess players)
{
    /// <summary>Every claim covering a block, in the order the world holds them.</summary>
    public IReadOnlyList<ClaimPayload> At(int x, int y, int z) =>
        // Null rather than empty where the region around a place holds no claims at
        // all, which is most of a world. Declared as though it never were.
        (api.World.Claims.Get(new BlockPos(x, y, z)) ?? [])
            .Select(Described)
            .ToList();

    /// <summary>
    /// One player's claims, numbered as the game numbers them. The order is the
    /// world's own claim list filtered to that owner, which is exactly what
    /// <c>/land list</c> walks, so a number read here is the number they are shown.
    /// </summary>
    public IReadOnlyList<ClaimPayload> Of(string player, ScriptOrigin origin)
    {
        // Resolved through the account store rather than the connected players, so a
        // script may tidy up after somebody who is not here — which is most of what
        // reading another player's claims is for.
        var owner = players.Account(player, origin).PlayerUID;
        return Owned(owner).Select((claim, index) => Described(claim, owner, index)).ToList();
    }

    /// <summary>
    /// Claims the land, and answers with the number the new claim was given. Saved
    /// with the world and sent to every client by the game itself, so a player sees it
    /// without having installed anything.
    /// </summary>
    public int Add(ClaimSpec spec, ScriptOrigin origin)
    {
        var owner = players.Account(spec.Owner, origin);

        var claim = new LandClaim
        {
            OwnedByPlayerUid = owner.PlayerUID,
            LastKnownOwnerName = owner.LastKnownPlayername,
            Description = spec.Description,
            ProtectionLevel = spec.ProtectionLevel,
            AllowUseEveryone = spec.AllowUseEveryone,
            AllowTraverseEveryone = spec.AllowTraverseEveryone,
            Areas = [Box(spec)],
        };

        api.World.Claims.Add(claim);

        // Read back rather than counted before the add: the number is a position in
        // the world's list, and reporting where it actually landed is what lets a
        // caller hand it straight to Remove.
        return Owned(owner.PlayerUID).Count - 1;
    }

    /// <summary>
    /// Takes back one claim, named the way a script was told about it, and says
    /// whether there was one to take. Every later claim of that owner moves down a
    /// number, which is the game's own behaviour rather than this mod's.
    /// </summary>
    public bool Remove(string player, int index, ScriptOrigin origin)
    {
        var owner = players.Account(player, origin).PlayerUID;
        var owned = Owned(owner);

        if (index < 0 || index >= owned.Count)
        {
            throw new ScriptError(origin, owned.Count == 0
                ? $"'{player}' has no claims to remove, so there is no claim {index}"
                : $"'{player}' has {owned.Count} claim(s), numbered 0 to {owned.Count - 1}, so there is no claim {index}");
        }

        return api.World.Claims.Remove(owned[index]);
    }

    /// <summary>
    /// One owner's claims in the world's own order, which every number a script sees
    /// counts along. Sole owner of that filter: reading a claim and
    /// removing it have to walk the same list in the same order or a number read from
    /// one would name a different claim to the other.
    /// </summary>
    private List<LandClaim> Owned(string owner) =>
        api.World.Claims.All.Where(claim => claim.OwnedByPlayerUid == owner).ToList();

    /// <summary>A claim as a script is told about it, given its owner and number.</summary>
    private static ClaimPayload Described(LandClaim claim, string owner, int index) => new()
    {
        Owner = owner,
        // Declared non-nullable and null on a claim the world generated rather than a
        // player made, which the game holds for the spawn protection it places itself.
        OwnerName = claim.LastKnownOwnerName ?? "",
        Index = index,
        Description = claim.Description ?? "",
        ProtectionLevel = claim.ProtectionLevel,
        Areas = claim.Areas.Select(Described).ToArray(),
        AllowUseEveryone = claim.AllowUseEveryone,
        AllowTraverseEveryone = claim.AllowTraverseEveryone,
        Permitted = claim.PermittedPlayerUids.Select(permit => Described(claim, permit)).ToArray(),
    };

    /// <summary>
    /// A claim found by position, whose number has to be worked out: a search by place
    /// says nothing about where the claim sits in its owner's list, and the number is
    /// half of what names it.
    /// </summary>
    private ClaimPayload Described(LandClaim claim) =>
        Described(claim, claim.OwnedByPlayerUid ?? "",
            Owned(claim.OwnedByPlayerUid ?? "").IndexOf(claim));

    /// <summary>One box of a claim, as two corners the way a script writes them.</summary>
    private static ClaimAreaPayload Described(Cuboidi area) => new()
    {
        X = area.MinX, Y = area.MinY, Z = area.MinZ,
        ToX = area.MaxX, ToY = area.MaxY, ToZ = area.MaxZ,
    };

    /// <summary>One player let onto a claim, and the three things they may separately do.</summary>
    private static PermitPayload Described(
        LandClaim claim, KeyValuePair<string, EnumBlockAccessFlags> permit) => new()
    {
        Player = permit.Key,
        Name = claim.PermittedPlayerLastKnownPlayerName.GetValueOrDefault(permit.Key, ""),
        MayBuild = permit.Value.HasFlag(EnumBlockAccessFlags.BuildOrBreak),
        MayUse = permit.Value.HasFlag(EnumBlockAccessFlags.Use),
        MayTraverse = permit.Value.HasFlag(EnumBlockAccessFlags.Traverse),
    };

    /// <summary>
    /// The box a script asked for, with its corners sorted. The game requires the
    /// lower corner to actually be lower, where a script naming two opposite corners
    /// has no reason to know which of them that is.
    /// </summary>
    private static Cuboidi Box(ClaimSpec spec) => new(
        System.Math.Min(spec.X, spec.ToX),
        System.Math.Min(spec.Y, spec.ToY),
        System.Math.Min(spec.Z, spec.ToZ),
        System.Math.Max(spec.X, spec.ToX),
        System.Math.Max(spec.Y, spec.ToY),
        System.Math.Max(spec.Z, spec.ToZ));
}
