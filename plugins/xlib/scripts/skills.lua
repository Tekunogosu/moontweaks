-- Skills: what XLib tracks for each player, reached through the xlib plugin.
--
-- `plugin.xlib` exists only on a server running the plugin, so the guard is what
-- lets this script sit in a folder shared with servers that do not.

if not plugin or not plugin.xlib then
  moontweaks.log.info("the xlib plugin is not loaded, so skills are out of reach")
  return
end

local skills = plugin.xlib

moontweaks.log.info("skills on this server: " .. table.concat(skills.all(), ", "))

-- /mylevel <name>: where the caller stands in one skill.
moontweaks.commands.add {
  name = "mylevel",
  description = "Report your level and experience in a skill",
  requiresPlayer = true,
  args = { { name = "skill", type = "word" } },
  handler = function(e)
    return ("%s: level %d, %.0f experience"):format(
      e.args.skill, skills.level(e.player, e.args.skill), skills.experience(e.player, e.args.skill))
  end,
}

-- /train <name> <amount>: experience given the caller, with XLib's multipliers applied.
moontweaks.commands.add {
  name = "train",
  description = "Give yourself experience in a skill",
  requiresPlayer = true,
  args = { { name = "skill", type = "word" }, { name = "amount", type = "number" } },
  handler = function(e)
    skills.addExperience(e.player, e.args.skill, e.args.amount)
    return ("%s is now level %d"):format(e.args.skill, skills.level(e.player, e.args.skill))
  end,
}
