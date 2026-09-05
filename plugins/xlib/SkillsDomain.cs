using System.Collections.Generic;
using System.Linq;
using MoonTweaks.Api;
using MoonTweaks.Scripting;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using XLib.XLeveling;

namespace MoonTweaks.Plugins.XLib;

/// <summary>
/// The skills XLib keeps for each player: what they are, how far a player has come
/// in one, and moving them along. Players are named by the identifier MoonTweaks
/// hands a script, and skills by the name XLib registered them under or the one it
/// displays them as.
/// </summary>
/// <example>
/// <code>
/// local skills = plugin.xlib
///
/// moontweaks.commands.add {
///   name = "mylevel",
///   description = "Report your level in a skill",
///   requiresPlayer = true,
///   args = { { name = "skill", type = "word" } },
///   handler = function(e)
///     return ("%s: level %d, %.0f experience"):format(
///       e.args.skill, skills.level(e.player, e.args.skill), skills.experience(e.player, e.args.skill))
///   end,
/// }
///
/// -- A skill named as XLib registered it, or as it displays it.
/// for _, name in ipairs(skills.all()) do
///   moontweaks.log.info("skill: " .. name)
/// end
/// </code>
/// </example>
[LuaModule("plugin.xlib")]
public sealed class SkillsDomain(ICoreServerAPI server)
{
    /// <summary>Every skill XLib has registered on this server, by the name it registered.</summary>
    /// <param name="origin">Script line asking.</param>
    [LuaFunction("all")]
    public IReadOnlyList<string> All(ScriptOrigin origin) =>
        Leveling(origin).SkillSetTemplate.Skills.Select(skill => skill.Name).ToList();

    /// <summary>A player's level in a skill.</summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="player">Identifier of the player, as an event hands it over.</param>
    /// <param name="skill">Name of the skill, as registered or as displayed.</param>
    [LuaFunction("level")]
    public int Level(ScriptOrigin origin, string player, string skill) =>
        PlayerSkill(origin, player, skill).Level;

    /// <summary>A player's experience in a skill, towards the next level.</summary>
    /// <param name="origin">Script line asking.</param>
    /// <param name="player">Identifier of the player, as an event hands it over.</param>
    /// <param name="skill">Name of the skill, as registered or as displayed.</param>
    [LuaFunction("experience")]
    public double Experience(ScriptOrigin origin, string player, string skill) =>
        PlayerSkill(origin, player, skill).Experience;

    /// <summary>
    /// Gives a player experience in a skill, with XLib's own multipliers applied, and
    /// tells their client so the change shows at once.
    /// </summary>
    /// <param name="origin">Script line requesting the change.</param>
    /// <param name="player">Identifier of the player, as an event hands it over.</param>
    /// <param name="skill">Name of the skill, as registered or as displayed.</param>
    /// <param name="amount">How much, before multipliers.</param>
    [LuaFunction("addExperience")]
    public void AddExperience(ScriptOrigin origin, string player, string skill, double amount) =>
        Api(origin).AddExperienceToPlayerSkill(Player(origin, player), Skill(origin, skill).Id, (float)amount);

    /// <summary>Sets a player's level in a skill outright, within the skill's own bounds.</summary>
    /// <param name="origin">Script line requesting the change.</param>
    /// <param name="player">Identifier of the player, as an event hands it over.</param>
    /// <param name="skill">Name of the skill, as registered or as displayed.</param>
    /// <param name="level">The level to set.</param>
    [LuaFunction("setLevel")]
    public void SetLevel(ScriptOrigin origin, string player, string skill, int level) =>
        Api(origin).SetPlayerSkillLevel(Player(origin, player), Skill(origin, skill).Id, level);

    private XLeveling Leveling(ScriptOrigin origin) =>
        XLeveling.Instance(server)
        ?? throw new ScriptError(origin, "reaching skills needs the 'xlibfork' mod, which this server does not have loaded");

    /// <summary>
    /// XLib's per-player side, which it builds in its server-side start. Scripts run
    /// before that, so a player function called from a script's top level rather than
    /// from a command or an event is told why rather than handed a null.
    /// </summary>
    private IXLevelingAPI Api(ScriptOrigin origin) =>
        Leveling(origin).IXLevelingAPI
        ?? throw new ScriptError(origin,
            "XLib has not started its player skills yet; reach them from a command or an event handler");

    private IPlayer Player(ScriptOrigin origin, string player) =>
        server.World.PlayerByUid(player)
        ?? throw new ScriptError(origin, $"no player is connected with the identifier '{player}'");

    private Skill Skill(ScriptOrigin origin, string skill) =>
        Leveling(origin).GetSkill(skill, allowDisplayName: true)
        ?? throw new ScriptError(origin, $"XLib has no skill named '{skill}'");

    private PlayerSkill PlayerSkill(ScriptOrigin origin, string player, string skill)
    {
        var set = Api(origin).GetPlayerSkillSet(Player(origin, player))
            ?? throw new ScriptError(origin, $"XLib holds no skills for the player '{player}'");

        return set.FindSkill(Skill(origin, skill).Name)
            ?? throw new ScriptError(origin, $"the player '{player}' has no '{skill}' skill");
    }
}
