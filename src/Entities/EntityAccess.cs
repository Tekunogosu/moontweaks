using System;
using System.Collections.Generic;
using System.Linq;
using MoonTweaks.Api;
using MoonTweaks.Assets;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace MoonTweaks.Entities;

/// <summary>
/// Reaching a living thing, and describing one. Sole owner of turning the identifier
/// a script holds into the entity the world is running, so the binding surface above
/// it is a list of what a script may do rather than a repetition of how to find
/// anything.
/// </summary>
/// <remarks>
/// An entity is named by the identifier the server gave it, in the same way a player
/// is named by theirs, and for the same reason: a script may hold one long after the
/// search that produced it, and the thing it named may be gone.
///
/// The two identifiers are not alike in one way worth knowing. A player's outlives
/// everything — they may be offline for a month and it still names them. An entity's
/// is good only while the entity is loaded: a chunk unloading takes it out of reach,
/// and nothing brings it back until that chunk returns.
/// </remarks>
public sealed class EntityAccess(ICoreServerAPI api)
{
    /// <summary>
    /// The entity an identifier names, or nothing where it names none. Sole owner of
    /// that lookup, so asking whether something is there and reaching for it are the
    /// same question asked once rather than the same question asked twice.
    /// </summary>
    public Entity? Loaded(double id) => api.World.GetEntityById((long)id);

    /// <summary>The entity an identifier names, or a failure saying it names nothing.</summary>
    public Entity Find(double id, ScriptOrigin origin) =>
        Loaded(id)
        ?? throw new ScriptError(origin,
            $"no entity is loaded with the identifier {(long)id}; "
            + "an identifier is only good while its entity is loaded");

    /// <summary>Whether an identifier still names something the world is running.</summary>
    public bool IsLoaded(double id) => Loaded(id) is not null;

    /// <summary>What a script is told about one entity.</summary>
    public static EntityPayload Describe(Entity entity)
    {
        // Read once: an entity that carries no health carries none for both of these,
        // and one that does answers the same behaviour to each.
        var health = entity.GetBehavior<EntityBehaviorHealth>();

        return new EntityPayload
        {
            Id = entity.EntityId,
            Code = entity.Code?.ToString() ?? "",
            // GetName is declared non-nullable and starts from null, returning it for
            // any behaviour that answers PreventSubsequent. The guard stays.
            Name = entity.GetName() ?? "",
            X = entity.Pos.X,
            Y = entity.Pos.Y,
            Z = entity.Pos.Z,
            Yaw = entity.Pos.Yaw * 180.0 / Math.PI,
            Alive = entity.Alive,
            OnFire = entity.IsOnFire,
            OnGround = entity.OnGround,
            Swimming = entity.Swimming,
            Health = health?.Health,
            MaxHealth = health?.MaxHealth,
            Player = (entity as EntityPlayer)?.PlayerUID,
            Stack = (entity as EntityItem)?.Itemstack is { } carried
                ? new StackPayload(carried.Collectible?.Code?.ToString() ?? "", carried.StackSize)
                : null,
        };
    }

    /// <summary>
    /// Everything in a box that the search asked for, nearest first so that taking
    /// the first of them is taking the closest.
    /// </summary>
    public IReadOnlyList<Entity> Around(AreaSpec area, ScriptOrigin origin)
    {
        var middle = new Vec3d(area.X, area.Y, area.Z);
        var wanted = area.Code is null ? null : new AssetLocation(area.Code);

        // Built once for the search rather than once per candidate: the names are
        // looked up in the registry, and a box may hold hundreds of creatures.
        var carrying = TagConditions.Build(api.EntityTagRegistry, area.Tags, origin, "tags");

        return [.. api.World
            .GetEntitiesAround(
                middle,
                (float)area.Range,
                (float)(area.Height ?? area.Range),
                entity => Wanted(entity, area, wanted, carrying))
            .OrderBy(entity => entity.Pos.SquareDistanceTo(middle))];
    }

    /// <summary>Whether one entity is the sort a search asked for.</summary>
    private static bool Wanted(
        Entity entity, AreaSpec area, AssetLocation? wanted, ComplexTagCondition<TagSetFast> carrying)
    {
        if (area.SkipPlayers && entity is EntityPlayer) return false;
        if (area.AliveOnly && !entity.Alive) return false;
        if (!carrying.IsEmpty && !carrying.Matches(entity.Tags)) return false;

        return wanted is null || (entity.Code is { } code && WildcardUtil.Match(wanted, code));
    }

    /// <summary>
    /// Puts entities into the world and says what they were given as identifiers.
    /// </summary>
    /// <remarks>
    /// The type is looked up first and refused by name if the server has none, which
    /// is the one thing here that can be checked before anything happens. Everything
    /// spawned in one call shares a herd, so a group moves together rather than
    /// scattering the moment it appears.
    /// </remarks>
    public IReadOnlyList<double> Spawn(SpawnSpec spec, ScriptOrigin origin)
    {
        var type = api.World.GetEntityType(new AssetLocation(spec.Code))
            ?? throw new ScriptError(origin, $"'{spec.Code}' is not a known entity");

        if (spec.Quantity < 1)
        {
            throw new ScriptError(origin, $"quantity must be at least 1, got {spec.Quantity}");
        }

        var herd = spec.Herd ? api.WorldManager.GetNextUniqueId() : 0;
        var random = api.World.Rand;
        var spawned = new List<double>(spec.Quantity);

        for (var made = 0; made < spec.Quantity; made++)
        {
            var entity = api.ClassRegistry.CreateEntity(type);

            if (spec.Herd && entity is EntityAgent agent) agent.HerdId = herd;

            entity.Pos.SetPos(
                spec.X + Scatter(random, spec.Spread),
                spec.Y,
                spec.Z + Scatter(random, spec.Spread));
            entity.Pos.Yaw = (float)(spec.Yaw * Math.PI / 180.0);
            entity.Pos.Pitch = 0;

            api.World.SpawnEntity(entity);
            spawned.Add(entity.EntityId);
        }

        return spawned;
    }

    /// <summary>How far one of a group lands from where the group was asked for.</summary>
    private static double Scatter(Random random, double spread) =>
        spread <= 0 ? 0 : random.NextDouble() * 2 * spread - spread;

    /// <summary>
    /// Kills something, as anything killing it would: it dies, and whatever it drops
    /// lands where it fell.
    /// </summary>
    public void Kill(double id, ScriptOrigin origin) =>
        Find(id, origin).Die(EnumDespawnReason.Death);

    /// <summary>
    /// Takes something out of the world without killing it. Nothing drops and nothing
    /// notices, which is the difference from <see cref="Kill"/>.
    /// </summary>
    public void Despawn(double id, ScriptOrigin origin) =>
        api.World.DespawnEntity(
            Find(id, origin), new EntityDespawnData { Reason = EnumDespawnReason.Removed });

    /// <summary>
    /// How much punishment something can take, and has taken. Named in the failure
    /// rather than returned as nothing, because a script asking for the health of
    /// something with none has made a mistake worth reading.
    /// </summary>
    public EntityBehaviorHealth Health(double id, ScriptOrigin origin)
    {
        var entity = Find(id, origin);

        return entity.GetBehavior<EntityBehaviorHealth>()
            ?? throw new ScriptError(origin,
                $"'{entity.Code}' has no health to read or change");
    }

    /// <summary>Hurts something, and says whether it took the damage.</summary>
    /// <remarks>
    /// Answering false is ordinary rather than a failure: something may be invulnerable,
    /// already dead, or still inside the moment of not being hurt twice by the same blow.
    /// </remarks>
    public bool Damage(double id, double amount, ScriptOrigin origin) =>
        Find(id, origin).ReceiveDamage(
            new DamageSource { Source = EnumDamageSource.Machine, Type = EnumDamageType.Injury },
            (float)amount);

    /// <summary>What something is called, or what it should be called from now on.</summary>
    /// <remarks>
    /// A name is a behaviour rather than a field, and only things meant to carry one
    /// have it — so naming a wolf works and naming a falling rock does not.
    /// </remarks>
    public void SetName(double id, string name, ScriptOrigin origin)
    {
        var entity = Find(id, origin);

        var tag = entity.GetBehavior<EntityBehaviorNameTag>()
            ?? throw new ScriptError(origin, $"'{entity.Code}' cannot carry a name");

        tag.SetName(name);
    }

    /// <summary>
    /// Remembers something against an entity, saved with it and still there when its
    /// chunk comes back.
    /// </summary>
    public void Remember(double id, string key, ScriptValue value, ScriptOrigin origin)
    {
        var held = Find(id, origin).WatchedAttributes;

        ScriptStore.Write(key, value, (name, json) =>
        {
            held.SetString(name, json);
            held.MarkPathDirty(name);
        });
    }

    /// <summary>What was remembered against an entity under a name, or nil when nothing was.</summary>
    public ScriptValue Recall(double id, string key, ScriptOrigin origin) =>
        ScriptStore.Read(key, name => Find(id, origin).WatchedAttributes.GetString(name));

    /// <summary>Adds or replaces one named contribution to an ability.</summary>
    public void SetStat(EntityStatSpec spec, ScriptOrigin origin) =>
        StatContribution.Set(
            Find(spec.Entity, origin).Stats, spec.Stat, spec.Name, spec.Value, spec.Persistent);

    /// <summary>Takes back one named contribution, leaving every other alone.</summary>
    public void ClearStat(double id, string stat, string name, ScriptOrigin origin) =>
        StatContribution.Clear(Find(id, origin).Stats, stat, name);
}
