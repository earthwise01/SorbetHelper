local fakeTilesHelper = require("helpers.fake_tiles")
local mods = require("mods")
local depths = mods.requireFromPlugin("libraries.depths")

local block = {}

block.name = "SorbetHelper/CrumbleOnFlagBlock"
block.depth = -10010
function block.placements()
    return {
        name = "crumble_on_flag_block",
        alternativeName = "crumble_wall_on_flag",
        data = {
            tiletype = fakeTilesHelper.getPlacementMaterial(),
            blendin = true,
            playAudio = true,
            showDebris = true,
            flag = "",
            inverted = false,
            depth = -10010,
            destroyAttached = false,
            fadeInTime = 1.0,
            width = 8,
            height = 8
        }
    }
end


local fieldInfo = {
    depth = {
        fieldType = "integer",
        options = depths.addDepth(depths.getDepths(), "Crumble Wall On Rumble", -10010),
        editable = true
    },
    fadeInTime = {
        minimumValue = 0
    }
}
block.fieldInformation = fakeTilesHelper.addTileFieldInformation(fieldInfo, "tiletype")
block.fieldOrder = {
    "x", "y", "width", "height", "depth", "fadeInTime", "flag", "tiletype", "playAudio", "destroyAttached", "blendin", "showDebris", "inverted"
}

block.sprite = fakeTilesHelper.getEntitySpriteFunction("tiletype", "blendin")

return block