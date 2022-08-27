local fakeTilesHelper = require("helpers.fake_tiles")

local block = {}

block.name = "SorbetHelper/CrumbleOnFlagBlock"
block.placements = {
    name = "crumble_on_flag_block",
    data = {
        tiletype = "3",
        blendin = true,
        playAudio = true,
        showDebris = true,
        flag = "",
        inverted = false,
        depth = -10010,
        width = 8,
        height = 8
    }
}

block.sprite = fakeTilesHelper.getEntitySpriteFunction("tiletype", "blendin")
block.fieldInformation = fakeTilesHelper.getFieldInformation("tiletype")

function block.depth(room, entity)
    return -10010
end

return block