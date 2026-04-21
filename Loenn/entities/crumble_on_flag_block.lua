local fakeTilesHelper = require("helpers.fake_tiles")
local sorbetHelper = require("mods").requireFromPlugin("libraries.sorbet_helper")

local crumbleOnFlagBlock = {}

crumbleOnFlagBlock.name = "SorbetHelper/CrumbleOnFlagBlock"
function crumbleOnFlagBlock.placements()
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

crumbleOnFlagBlock.fieldOrder = {
    "x", "y",
    "width", "height",
    "depth", "fadeInTime",
    "flag", "tiletype",
    "playAudio", "destroyAttached", "blendin", "showDebris",
    "inverted"
}

crumbleOnFlagBlock.fieldInformation = fakeTilesHelper.addTileFieldInformation({
    depth = {
        fieldType = "integer",
        options = sorbetHelper.getDepths({
            {"Crumble Wall On Rumble", -10010}
        }),
        editable = true
    },
    fadeInTime = {
        minimumValue = 0
    }
}, "tiletype")

crumbleOnFlagBlock.sprite = fakeTilesHelper.getEntitySpriteFunction("tiletype", "blendin")

function crumbleOnFlagBlock.depth(room, entity)
    return entity.depth or -10010
end

return crumbleOnFlagBlock
