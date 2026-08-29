using System;
using System.Collections.Generic;
using MoonTweaks.Api;
using MoonTweaks.Assets;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace MoonTweaks.Players;

/// <summary>
/// What a script may do to a player. Players are named by the identifier an event
/// hands a script, rather than passed as an object, so nothing a script holds
/// outlives the player it refers to.
/// </summary>
[LuaModule("moontweaks.players")]
public sealed class PlayerDomain(PlayerAccess players, AssetStacks stacks)
{
    /// <summary>
    /// Moves where a player will respawn. Survives a restart, because the game saves
    /// it with the player rather than the world.
    /// </summary>
    /// <param name="origin">Script line requesting the change.</param>
    /// <param name="player">Identifier of the player, as an event gives it.</param>
    /// <param name="x">Block position to respawn at.</param>
    /// <param name="y">Block position to respawn at.</param>
    /// <param name="z">Block position to respawn at.</param>
    [LuaFunction("setSpawn")]
    public void SetSpawn(ScriptOrigin origin, string player, int x, int y, int z) =>
        players.Find(player, origin).SetSpawnPosition(new PlayerSpawnPos { x = x, y = y, z = z });

    /// <summary>
    /// Clears a player's own spawn, so they return to the world's instead.
    /// </summary>
    /// <param name="origin">Script line requesting the change.</param>
    /// <param name="player">Identifier of the player, as an event gives it.</param>
    [LuaFunction("clearSpawn")]
    public void ClearSpawn(ScriptOrigin origin, string player) =>
        players.Find(player, origin).ClearSpawnPosition();

    /// <summary>
    /// Sends one player a message in their chat.
    /// </summary>
    /// <param name="origin">Script line sending the message.</param>
    /// <param name="player">Identifier of the player, as an event gives it.</param>
    /// <param name="message">Text to send.</param>
    [LuaFunction("say")]
    public void Say(ScriptOrigin origin, string player, string message) =>
        players.Find(player, origin).SendMessage(GlobalConstants.GeneralChatGroup, message, EnumChatType.Notification);

    /// <summary>
    /// Hands a stack of something to a player, and says whether all of it reached
    /// them. The game puts it wherever it fits; a full inventory takes none of it and
    /// answers false, which is a thing to tell them about rather than a mistake.
    /// Anything that did not fit is gone, so a script that must not lose it should
    /// drop what is left where they stand.
    /// </summary>
    /// <param name="origin">Script line handing it over.</param>
    /// <param name="player">Identifier of the player, as an event gives it.</param>
    /// <param name="stack">What to give them, which a bare code names one of.</param>
    [LuaFunction("give")]
    public bool Give(ScriptOrigin origin, string player, ItemStackSpec stack) =>
        players.Give(player, stacks.Resolved(stack, origin, "stack"), origin);

    /// <summary>
    /// Where a player is, as a table of <c>x</c>, <c>y</c> and <c>z</c>.
    /// </summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="player">Identifier of the player, as an event gives it.</param>
    [LuaFunction("position")]
    public VectorPayload Position(ScriptOrigin origin, string player)
    {
        var at = players.Find(player, origin).Entity.Pos;
        return new VectorPayload(at.X, at.Y, at.Z);
    }

    /// <summary>
    /// Which way a player is looking, as a table of <c>x</c>, <c>y</c> and <c>z</c>
    /// one block long. Multiplying it by a speed is how something is thrown the way
    /// they are facing rather than in a fixed direction.
    /// </summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="player">Identifier of the player, as an event gives it.</param>
    [LuaFunction("facing")]
    public VectorPayload Facing(ScriptOrigin origin, string player)
    {
        var view = players.Find(player, origin).Entity.Pos.GetViewVector();
        return new VectorPayload(view.X, view.Y, view.Z);
    }

    /// <summary>Moves a player somewhere else.</summary>
    /// <param name="origin">Script line requesting the move.</param>
    /// <param name="player">Identifier of the player, as an event gives it.</param>
    /// <param name="x">Where to put them.</param>
    /// <param name="y">Where to put them.</param>
    /// <param name="z">Where to put them.</param>
    [LuaFunction("teleport")]
    public void Teleport(ScriptOrigin origin, string player, double x, double y, double z) =>
        players.Find(player, origin).Entity.TeleportToDouble(x, y, z);

    /// <summary>How much health a player has left.</summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="player">Identifier of the player, as an event gives it.</param>
    [LuaFunction("health")]
    public float Health(ScriptOrigin origin, string player) => players.Health(player, origin).Health;

    /// <summary>How much health a player can have.</summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="player">Identifier of the player, as an event gives it.</param>
    [LuaFunction("maxHealth")]
    public float MaxHealth(ScriptOrigin origin, string player) => players.Health(player, origin).MaxHealth;

    /// <summary>
    /// Sets how much health a player has. Above their maximum is kept, because the
    /// game clamps it itself the moment anything changes it.
    /// </summary>
    /// <param name="origin">Script line requesting the change.</param>
    /// <param name="player">Identifier of the player, as an event gives it.</param>
    /// <param name="health">What to set it to.</param>
    [LuaFunction("setHealth")]
    public void SetHealth(ScriptOrigin origin, string player, double health) =>
        players.Health(player, origin).Health = (float)health;

    /// <summary>
    /// How full a player is. The same quantity a food's <c>satiety</c> adds; the game
    /// itself calls this one saturation, on the player rather than on the food.
    /// </summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="player">Identifier of the player, as an event gives it.</param>
    [LuaFunction("satiety")]
    public float Satiety(ScriptOrigin origin, string player) => players.Hunger(player, origin).Saturation;

    /// <summary>How full a player can be.</summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="player">Identifier of the player, as an event gives it.</param>
    [LuaFunction("maxSatiety")]
    public float MaxSatiety(ScriptOrigin origin, string player) => players.Hunger(player, origin).MaxSaturation;

    /// <summary>Sets how full a player is.</summary>
    /// <param name="origin">Script line requesting the change.</param>
    /// <param name="player">Identifier of the player, as an event gives it.</param>
    /// <param name="satiety">What to set it to.</param>
    [LuaFunction("setSatiety")]
    public void SetSatiety(ScriptOrigin origin, string player, double satiety) =>
        players.Hunger(player, origin).Saturation = (float)satiety;

    /// <summary>Which mode a player is playing in.</summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="player">Identifier of the player, as an event gives it.</param>
    [LuaFunction("gameMode")]
    public EnumPlayKind GameMode(ScriptOrigin origin, string player) =>
        ValueSet.As<EnumPlayKind>(players.Find(player, origin).WorldData.CurrentGameMode);

    /// <summary>Moves a player to another mode.</summary>
    /// <param name="origin">Script line requesting the change.</param>
    /// <param name="player">Identifier of the player, as an event gives it.</param>
    /// <param name="mode">Mode to put them in.</param>
    [LuaFunction("setGameMode")]
    public void SetGameMode(ScriptOrigin origin, string player, EnumPlayKind mode) =>
        players.Find(player, origin).WorldData.CurrentGameMode = ValueSet.As<EnumGameMode>(mode);

    /// <summary>
    /// Remembers something about a player, saved with them rather than with the world
    /// and so still there after a restart. Any value a script can write is kept,
    /// including a table.
    /// </summary>
    /// <param name="origin">Script line storing it.</param>
    /// <param name="player">Identifier of the player, as an event gives it.</param>
    /// <param name="key">Name to store it under.</param>
    /// <param name="value">What to store.</param>
    [LuaFunction("setData")]
    public void SetData(ScriptOrigin origin, string player, string key, ScriptValue value) =>
        players.Find(player, origin).SetModData(Scoped(key), ScriptJson.Write(value));

    /// <summary>
    /// What was remembered about a player under a name, or nil when nothing was.
    /// </summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="player">Identifier of the player, as an event gives it.</param>
    /// <param name="key">Name it was stored under.</param>
    [LuaFunction("getData")]
    public ScriptValue GetData(ScriptOrigin origin, string player, string key) =>
        ScriptJson.Parse(players.Find(player, origin).GetModData<string?>(Scoped(key), null));

    /// <summary>
    /// Keys are stored under this mod's own prefix, so a script cannot read or
    /// overwrite what another mod saved on the same player.
    /// </summary>
    private static string Scoped(string key) => $"moontweaks:{key}";

    /// <summary>How tired a player is, from nothing to needing sleep.</summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="player">Identifier of the player, as an event gives it.</param>
    [LuaFunction("tiredness")]
    public float Tiredness(ScriptOrigin origin, string player) =>
        players.Tiredness(player, origin).Tiredness;

    /// <summary>Sets how tired a player is.</summary>
    /// <param name="origin">Script line requesting the change.</param>
    /// <param name="player">Identifier of the player, as an event gives it.</param>
    /// <param name="tiredness">What to set it to.</param>
    [LuaFunction("setTiredness")]
    public void SetTiredness(ScriptOrigin origin, string player, double tiredness) =>
        players.Tiredness(player, origin).Tiredness = (float)tiredness;

    /// <summary>Whether a player is asleep in a bed right now.</summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="player">Identifier of the player, as an event gives it.</param>
    [LuaFunction("isSleeping")]
    public bool IsSleeping(ScriptOrigin origin, string player) =>
        players.Tiredness(player, origin).IsSleeping;

    /// <summary>
    /// How much of each kind of food a player has eaten lately, as a table keyed by
    /// the same names a food's own category uses.
    /// </summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="player">Identifier of the player, as an event gives it.</param>
    [LuaFunction("nutrition")]
    public ScriptValue Nutrition(ScriptOrigin origin, string player)
    {
        var hunger = players.Hunger(player, origin);
        return new ScriptValue.Map(new Dictionary<string, ScriptValue>
        {
            ["fruit"] = new ScriptValue.Num(hunger.FruitLevel),
            ["vegetable"] = new ScriptValue.Num(hunger.VegetableLevel),
            ["protein"] = new ScriptValue.Num(hunger.ProteinLevel),
            ["grain"] = new ScriptValue.Num(hunger.GrainLevel),
            ["dairy"] = new ScriptValue.Num(hunger.DairyLevel),
        });
    }

    /// <summary>
    /// Every player on the server right now, as the identifiers everything else here
    /// takes. The only source of one that is not an event, so this is what anything
    /// addressed to everybody is written from.
    /// </summary>
    /// <param name="origin">Script line asking.</param>
    [LuaFunction("all")]
    public IReadOnlyList<string> All(ScriptOrigin origin) => players.Online();

    /// <summary>
    /// Whether an identifier names somebody who is here. Worth asking before anything
    /// that reaches a player's body, since a script may have remembered an identifier
    /// long before it came to use it.
    /// </summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="player">Identifier of the player.</param>
    [LuaFunction("isOnline")]
    public bool IsOnline(ScriptOrigin origin, string player) => players.IsOnline(player);

    /// <summary>What a player is called, for putting into a message somebody will read.</summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="player">Identifier of the player, as an event gives it.</param>
    [LuaFunction("name")]
    public string Name(ScriptOrigin origin, string player) => players.Find(player, origin).PlayerName;

    /// <summary>
    /// The identifier of whoever last went by a name, or nil where the server has
    /// never seen it. This is how a name somebody typed becomes something the rest of
    /// this module accepts.
    /// </summary>
    /// <remarks>
    /// Answers for players who are not here, unlike everything else in this module.
    /// What is stored against them can still be read and written; nothing that
    /// reaches their body can, and will say so.
    /// </remarks>
    /// <param name="origin">Script line asking.</param>
    /// <param name="name">Name as it is spelled in game.</param>
    [LuaFunction("uidOf")]
    public string? UidOf(ScriptOrigin origin, string name) => players.UidOf(name);

    /// <summary>
    /// Sends one message to everybody on the server. Needs no list of players and no
    /// event to have happened, which is what makes it the way to announce anything.
    /// </summary>
    /// <param name="origin">Script line announcing it.</param>
    /// <param name="message">Text to send.</param>
    [LuaFunction("announce")]
    public void Announce(ScriptOrigin origin, string message) => players.Announce(message);

    /// <summary>
    /// Sends one player a message in the style the game uses for things that went
    /// wrong, so it reads as a refusal rather than as news.
    /// </summary>
    /// <param name="origin">Script line sending it.</param>
    /// <param name="player">Identifier of the player, as an event gives it.</param>
    /// <param name="message">Text to send.</param>
    [LuaFunction("warn")]
    public void Warn(ScriptOrigin origin, string player, string message) =>
        players.Find(player, origin)
            .SendMessage(GlobalConstants.GeneralChatGroup, message, EnumChatType.CommandError);

    /// <summary>
    /// Whether a player holds a privilege, such as <c>build</c> or <c>controlserver</c>.
    /// Reading one grants nothing; it is how a script gates its own behaviour on what
    /// the server has already decided about somebody.
    /// </summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="player">Identifier of the player, as an event gives it.</param>
    /// <param name="privilege">Name of the privilege.</param>
    [LuaFunction("hasPrivilege")]
    public bool HasPrivilege(ScriptOrigin origin, string player, string privilege) =>
        players.Find(player, origin).HasPrivilege(privilege);

    /// <summary>Every privilege a player holds.</summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="player">Identifier of the player, as an event gives it.</param>
    [LuaFunction("privileges")]
    public IReadOnlyList<string> Privileges(ScriptOrigin origin, string player) =>
        players.Find(player, origin).Privileges;

    /// <summary>
    /// What one of a player's abilities currently comes to, with everything affecting
    /// it added up. An ability nothing has touched reads 1, so the answer is always a
    /// multiplier of the ordinary.
    /// </summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="player">Identifier of the player, as an event gives it.</param>
    /// <param name="stat">Which ability, such as <c>walkspeed</c>.</param>
    [LuaFunction("stat")]
    public float Stat(ScriptOrigin origin, string player, string stat) =>
        players.Stat(player, stat, origin);

    /// <summary>
    /// Changes one of a player's abilities, under a name the same script uses to
    /// change or remove it later. This is how a temporary effect is given: a name
    /// nobody else uses, set when it starts and cleared when it ends.
    /// </summary>
    /// <param name="origin">Script line setting it.</param>
    /// <param name="stat">Whose ability, which one, by how much, and for how long.</param>
    [LuaFunction("setStat")]
    public void SetStat(ScriptOrigin origin, StatSpec stat) => players.SetStat(stat, origin);

    /// <summary>
    /// Takes back a change made under a name, leaving every other contribution to the
    /// same ability alone.
    /// </summary>
    /// <param name="origin">Script line taking it back.</param>
    /// <param name="player">Identifier of the player, as an event gives it.</param>
    /// <param name="stat">Which ability it was set on.</param>
    /// <param name="name">Name it was set under.</param>
    [LuaFunction("clearStat")]
    public void ClearStat(ScriptOrigin origin, string player, string stat, string name) =>
        players.ClearStat(player, stat, name, origin);

    /// <summary>
    /// The block a player has their cursor on, or nil where they are pointing at
    /// nothing. Answers without waiting for them to do anything to it, which is what
    /// a command about "the block in front of me" needs.
    /// </summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="player">Identifier of the player, as an event gives it.</param>
    [LuaFunction("looking")]
    public LookingPayload? Looking(ScriptOrigin origin, string player) =>
        players.Looking(player, origin);
}
