using System.Collections.Generic;
using System.Linq;
using MoonTweaks.Api;
using MoonTweaks.Assets;
using MoonTweaks.Scripting;

namespace MoonTweaks.Entities;

/// <summary>
/// The living things in a world, and the stacks lying on its floor. Everything that
/// is not a player, which <c>moontweaks.players</c> reaches instead.
/// </summary>
/// <remarks>
/// An entity is named by the identifier a search hands back, rather than passed as an
/// object, in the same way a player is. The identifiers differ in one way worth
/// knowing: a player's outlives everything, where an entity's is good only while the
/// entity is loaded. A script remembering one across a restart, or while its chunk was
/// away, should ask <c>isLoaded</c> before reaching for it.
///
/// These act on a loaded world, so they belong in a handler rather than in a script's
/// body: when scripts run, the recipes exist but the world does not.
/// </remarks>
/// <example>
/// <code>
/// local entities = moontweaks.entities
///
/// for _, wolf in ipairs(entities.around { x = 500, y = 110, z = 500, range = 30,
///                                         code = "game:wolf-adult", aliveOnly = true }) do
///   moontweaks.log.info(("%s at %.0f %.0f %.0f"):format(wolf.name, wolf.x, wolf.y, wolf.z))
/// end
///
/// entities.spawn { code = "game:chicken-hen", x = 500, y = 111, z = 500, quantity = 3 }
/// </code>
/// </example>
[LuaModule("moontweaks.entities")]
public sealed class EntityDomain(EntityAccess entities, AssetStacks stacks)
{
    /// <summary>
    /// Everything in a box around a point, nearest first. Players are skipped unless
    /// the search asks for them, since a script wanting a person has a better module
    /// to ask.
    /// </summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="area">Where to look, how far, and what to count.</param>
    [LuaFunction("around")]
    public IReadOnlyList<EntityPayload> Around(ScriptOrigin origin, AreaSpec area) =>
        [.. entities.Around(area, origin).Select(EntityAccess.Describe)];

    /// <summary>
    /// The closest thing a search matches, or nil where it matches nothing. The same
    /// search <c>around</c> takes, answered with one rather than all of them.
    /// </summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="area">Where to look, how far, and what to count.</param>
    [LuaFunction("nearest")]
    public EntityPayload? Nearest(ScriptOrigin origin, AreaSpec area) =>
        entities.Around(area, origin).Select(EntityAccess.Describe).FirstOrDefault();

    /// <summary>
    /// How many things a search matches. Cheaper than counting what <c>around</c>
    /// hands back, since nothing is described that is only going to be counted.
    /// </summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="area">Where to look, how far, and what to count.</param>
    [LuaFunction("count")]
    public int Count(ScriptOrigin origin, AreaSpec area) => entities.Around(area, origin).Count;

    /// <summary>What one entity is, or nil where the identifier names nothing loaded.</summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="entity">Identifier of the entity, as a search gives it.</param>
    [LuaFunction("get")]
    public EntityPayload? Get(ScriptOrigin origin, double entity) =>
        entities.Loaded(entity) is { } found ? EntityAccess.Describe(found) : null;

    /// <summary>
    /// Whether an identifier still names something the world is running. Worth asking
    /// before anything else when the identifier was remembered rather than just found:
    /// a chunk unloading takes an entity out of reach without telling anybody.
    /// </summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="entity">Identifier of the entity.</param>
    [LuaFunction("isLoaded")]
    public bool IsLoaded(ScriptOrigin origin, double entity) => entities.IsLoaded(entity);

    /// <summary>
    /// Puts things into the world and hands back their identifiers, one per thing.
    /// </summary>
    /// <remarks>
    /// The entity code is checked as this runs, so a code the server does not have
    /// names itself rather than quietly spawning nothing — which is as close to
    /// load-time checking as anything acting on a live world can get.
    /// </remarks>
    /// <param name="origin">Script line spawning them.</param>
    /// <param name="spawn">What to put there, where, how many, and how scattered.</param>
    [LuaFunction("spawn")]
    public IReadOnlyList<double> Spawn(ScriptOrigin origin, SpawnSpec spawn) =>
        entities.Spawn(spawn, origin);

    /// <summary>
    /// Kills something, as anything killing it would: it dies, and whatever it drops
    /// lands where it fell.
    /// </summary>
    /// <param name="origin">Script line killing it.</param>
    /// <param name="entity">Identifier of the entity, as a search gives it.</param>
    [LuaFunction("kill")]
    public void Kill(ScriptOrigin origin, double entity) => entities.Kill(entity, origin);

    /// <summary>
    /// Takes something out of the world without killing it. Nothing drops and nothing
    /// notices, which is what clearing up after a script wants rather than
    /// <c>kill</c>.
    /// </summary>
    /// <param name="origin">Script line removing it.</param>
    /// <param name="entity">Identifier of the entity, as a search gives it.</param>
    [LuaFunction("despawn")]
    public void Despawn(ScriptOrigin origin, double entity) => entities.Despawn(entity, origin);

    /// <summary>Moves something somewhere else.</summary>
    /// <param name="origin">Script line moving it.</param>
    /// <param name="entity">Identifier of the entity, as a search gives it.</param>
    /// <param name="x">Where to put it.</param>
    /// <param name="y">Where to put it.</param>
    /// <param name="z">Where to put it.</param>
    [LuaFunction("teleport")]
    public void Teleport(ScriptOrigin origin, double entity, double x, double y, double z) =>
        entities.Find(entity, origin).TeleportToDouble(x, y, z);

    /// <summary>How much health something has left.</summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="entity">Identifier of the entity, as a search gives it.</param>
    [LuaFunction("health")]
    public float Health(ScriptOrigin origin, double entity) => entities.Health(entity, origin).Health;

    /// <summary>How much health something can have.</summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="entity">Identifier of the entity, as a search gives it.</param>
    [LuaFunction("maxHealth")]
    public float MaxHealth(ScriptOrigin origin, double entity) =>
        entities.Health(entity, origin).MaxHealth;

    /// <summary>Sets how much health something has.</summary>
    /// <param name="origin">Script line requesting the change.</param>
    /// <param name="entity">Identifier of the entity, as a search gives it.</param>
    /// <param name="health">What to set it to.</param>
    [LuaFunction("setHealth")]
    public void SetHealth(ScriptOrigin origin, double entity, double health) =>
        entities.Health(entity, origin).Health = (float)health;

    /// <summary>
    /// Hurts something, and says whether it took the damage. Answering false is
    /// ordinary rather than a failure: it may be invulnerable, already dead, or still
    /// inside the moment that stops one blow landing twice.
    /// </summary>
    /// <param name="origin">Script line hurting it.</param>
    /// <param name="entity">Identifier of the entity, as a search gives it.</param>
    /// <param name="amount">How much damage to deal.</param>
    [LuaFunction("damage")]
    public bool Damage(ScriptOrigin origin, double entity, double amount) =>
        entities.Damage(entity, amount, origin);

    /// <summary>Sets something on fire.</summary>
    /// <param name="origin">Script line lighting it.</param>
    /// <param name="entity">Identifier of the entity, as a search gives it.</param>
    [LuaFunction("ignite")]
    public void Ignite(ScriptOrigin origin, double entity) =>
        entities.Find(entity, origin).IsOnFire = true;

    /// <summary>Puts something out.</summary>
    /// <param name="origin">Script line putting it out.</param>
    /// <param name="entity">Identifier of the entity, as a search gives it.</param>
    [LuaFunction("extinguish")]
    public void Extinguish(ScriptOrigin origin, double entity) =>
        entities.Find(entity, origin).IsOnFire = false;

    /// <summary>What something is called, which is its name tag where it has one.</summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="entity">Identifier of the entity, as a search gives it.</param>
    [LuaFunction("name")]
    public string Name(ScriptOrigin origin, double entity) =>
        // Declared non-nullable by the game and null in practice, as in EntityAccess.
        entities.Find(entity, origin).GetName() ?? "";

    /// <summary>
    /// Names something. Only things meant to carry a name can, so naming a wolf works
    /// and naming a falling rock is refused rather than ignored.
    /// </summary>
    /// <param name="origin">Script line naming it.</param>
    /// <param name="entity">Identifier of the entity, as a search gives it.</param>
    /// <param name="name">What to call it.</param>
    [LuaFunction("setName")]
    public void SetName(ScriptOrigin origin, double entity, string name) =>
        entities.SetName(entity, name, origin);

    /// <summary>
    /// Hands a stack to something, and says whether it took it. Most things take
    /// nothing; this is for the ones that carry an inventory.
    /// </summary>
    /// <param name="origin">Script line handing it over.</param>
    /// <param name="entity">Identifier of the entity, as a search gives it.</param>
    /// <param name="stack">What to give it, which a bare code names one of.</param>
    [LuaFunction("give")]
    public bool Give(ScriptOrigin origin, double entity, ItemStackSpec stack) =>
        entities.Find(entity, origin)
            .TryGiveItemStack(stacks.Resolved(stack, origin, "stack"));

    /// <summary>
    /// Remembers something against an entity, saved with it and still there when its
    /// chunk comes back. The counterpart of <c>moontweaks.players.setWorldData</c>.
    /// </summary>
    /// <param name="origin">Script line storing it.</param>
    /// <param name="entity">Identifier of the entity, as a search gives it.</param>
    /// <param name="key">Name to store it under.</param>
    /// <param name="value">What to store. Any value a script can write, a table included.</param>
    [LuaFunction("setData")]
    public void SetData(ScriptOrigin origin, double entity, string key, ScriptValue value) =>
        entities.Remember(entity, key, value, origin);

    /// <summary>What was remembered against an entity under a name, or nil when nothing was.</summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="entity">Identifier of the entity, as a search gives it.</param>
    /// <param name="key">Name it was stored under.</param>
    [LuaFunction("getData")]
    public ScriptValue GetData(ScriptOrigin origin, double entity, string key) =>
        entities.Recall(entity, key, origin);

    /// <summary>
    /// What one of an entity's abilities currently comes to, with everything affecting
    /// it added up. An ability nothing has touched reads 1.
    /// </summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="entity">Identifier of the entity, as a search gives it.</param>
    /// <param name="stat">Which ability, such as <c>walkspeed</c>.</param>
    [LuaFunction("stat")]
    public float Stat(ScriptOrigin origin, double entity, string stat) =>
        entities.Find(entity, origin).Stats.GetBlended(stat);

    /// <summary>
    /// Changes one of an entity's abilities, under a name the same script uses to
    /// change or remove it later.
    /// </summary>
    /// <param name="origin">Script line setting it.</param>
    /// <param name="stat">Whose ability, which one, by how much, and for how long.</param>
    [LuaFunction("setStat")]
    public void SetStat(ScriptOrigin origin, EntityStatSpec stat) => entities.SetStat(stat, origin);

    /// <summary>
    /// Takes back a change made under a name, leaving every other contribution to the
    /// same ability alone.
    /// </summary>
    /// <param name="origin">Script line taking it back.</param>
    /// <param name="entity">Identifier of the entity, as a search gives it.</param>
    /// <param name="stat">Which ability it was set on.</param>
    /// <param name="name">Name it was set under.</param>
    [LuaFunction("clearStat")]
    public void ClearStat(ScriptOrigin origin, double entity, string stat, string name) =>
        entities.ClearStat(entity, stat, name, origin);
}
