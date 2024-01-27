local fakeTilesHelper = require("helpers.fake_tiles")

local block = {}

block.name = "SorbetHelper/CrumbleOnFlagBlock"
block.depth = -10010
function block.placements()
    return {
        name = "crumble_on_flag_block",
        data = {
            tiletype = fakeTilesHelper.getPlacementMaterial(),
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
end

block.sprite = fakeTilesHelper.getEntitySpriteFunction("tiletype", "blendin")
block.fieldInformation = fakeTilesHelper.getFieldInformation("tiletype")

return block