-- Making a block give off light.
--
-- The game stores a block's light as three bytes: a hue and a saturation naming the
-- colour, and a brightness deciding how far it reaches. Brightness is what makes it
-- a light source at all — zero gives off nothing whatever the other two say — so
-- clearing a glow means writing a brightness of 0 rather than leaving the key out.
--
-- Each part runs from 0 to 255 and is refused outside that range rather than
-- silently wrapping round to a colour nobody asked for.

local blocks = moontweaks.blocks

-- Warm white, reaching about two thirds as far as the brightest light in the game.
blocks.set {
  code = "game:rock-granite",
  light = { hue = 30, saturation = 60, brightness = 16 },
}

-- Colour comes from hue and saturation together: a high saturation is a strong
-- colour, and a saturation of 0 is plain white whatever the hue says.
blocks.set {
  code = "game:glass-plain",
  light = { hue = 160, saturation = 200, brightness = 12 },
}

-- Putting one out. The colour is still written, and still means nothing, because
-- brightness is what decides whether there is any light to colour.
blocks.set {
  code = "game:crystal-*",
  light = { brightness = 0 },
}

moontweaks.log.info("block light done")
