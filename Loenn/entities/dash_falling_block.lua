local fakeTilesHelper = require("helpers.fake_tiles")

local fallingBlock = {}

fallingBlock.name = "SorbetHelper/DashFallingBlock"
fallingBlock.placements = {
    name = "falling_block",
    data = {
        tiletype = "3",
        depth = -9000,
        shakeSfx = "event:/game/general/fallblock_shake",
        impactSfx = "event:/game/general/fallblock_impact",
        fallOnTouch = false,
        climbFall = true,
        fallOnStaticMover = false,
        allowWavedash = false,
        width = 8,
        height = 8
    }
}

fallingBlock.sprite = fakeTilesHelper.getEntitySpriteFunction("tiletype", false)

fallingBlock.fieldOrder = {"x", "y", "width", "height", "shakeSfx", "impactSfx", "tiletype", "depth", "fallOnTouch", "climbFall", "fallOnStaticMover", "allowWavedash"}

function fallingBlock.depth(room, entity)
    return entity.depth
end

return fallingBlock