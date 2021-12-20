local fakeTilesHelper = require("helpers.fake_tiles")

local fallingBlock = {}

fallingBlock.name = "SorbetHelper/DashFallingBlock"
fallingBlock.placements = {
    name = "falling_block",
    data = {
        tiletype = "3",
        climbFall = true,
        behind = false,
        shakeSfx = "event:/game/general/fallblock_shake",
        impactSfx = "event:/game/general/fallblock_impact",
        width = 8,
        height = 8
    }
}

fallingBlock.sprite = fakeTilesHelper.getEntitySpriteFunction("tiletype", false)

function fallingBlock.depth(room, entity)
    return entity.behind and 5000 or 0
end

return fallingBlock