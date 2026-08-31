using System.Collections.Generic;
using System.Linq;
using MoonTweaks.Api;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;

namespace MoonTweaks.Host;

/// <summary>What a script is told about one other mod on this server.</summary>
/// <param name="mod">The mod as the loader holds it.</param>
[LuaTable("ModInfo", Given = true)]
public sealed class ModPayload(Mod mod)
{
    /// <summary>Identifier the mod is known by, which is what names it everywhere else.</summary>
    [LuaField("id")]
    public string Id { get; } = mod.Info?.ModID ?? "";

    /// <summary>Name the mod calls itself, for saying something a person will read.</summary>
    [LuaField("name")]
    public string Name { get; } = mod.Info?.Name ?? "";

    /// <summary>Version it declares, as it wrote it. Nil where it declares none.</summary>
    [LuaField("version")]
    public string? Version { get; } = mod.Info?.Version;
}

/// <summary>
/// The other mods this server is running. Whether one is present is the question a
/// script asks before naming anything that belongs to it.
/// </summary>
/// <remarks>
/// This exists because every other binding is strict on purpose. A recipe or an
/// <c>items.set</c> naming a code the server does not have refuses the whole run,
/// which is what stops a typo becoming a silently missing recipe — but it also means
/// a script written for two servers, one with a mod and one without, cannot simply
/// name that mod's items and hope. Asking first is how such a script is written:
/// guard the block with <c>isEnabled</c> and the codes inside it are only ever read
/// on a server that has them.
/// </remarks>
/// <example>
/// <code>
/// local mods = moontweaks.mods
///
/// -- Ask first, then name that mod's codes: a code the server does not have refuses
/// -- the whole run, so the guard is what makes one script work on two servers.
/// if mods.isEnabled("primitivesurvival") then
///   moontweaks.recipes.grid.remove("primitivesurvival:trap-basket")
/// end
///
/// for _, mod in ipairs(mods.all()) do
///   moontweaks.log.info(("%s (%s)"):format(mod.name, mod.id))
/// end
/// </code>
/// </example>
[LuaModule("moontweaks.mods")]
public sealed class ModDomain(IModLoader loader)
{
    /// <summary>
    /// Whether a mod is loaded on this server, named by its identifier rather than by
    /// its title — <c>primitivesurvival</c> rather than <c>Primitive Survival</c>.
    /// </summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="id">Identifier of the mod, as its own <c>modinfo.json</c> spells it.</param>
    [LuaFunction("isEnabled")]
    public bool IsEnabled(ScriptOrigin origin, string id) => loader.IsModEnabled(id);

    /// <summary>
    /// What a mod says about itself, or nil where it is not loaded. The nil is the
    /// point: this and <c>isEnabled</c> answer the same question, and a script that
    /// wants the version was going to ask both.
    /// </summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="id">Identifier of the mod.</param>
    [LuaFunction("get")]
    public ModPayload? Get(ScriptOrigin origin, string id) =>
        loader.GetMod(id) is { } mod ? new ModPayload(mod) : null;

    /// <summary>
    /// Every mod loaded here, in the order the loader holds them. This mod is among
    /// them, as is the game's own content.
    /// </summary>
    /// <param name="origin">Script line asking.</param>
    [LuaFunction("all")]
    public IReadOnlyList<ModPayload> All(ScriptOrigin origin) =>
        [.. loader.Mods.Select(mod => new ModPayload(mod))];
}
