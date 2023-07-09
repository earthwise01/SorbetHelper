local fakeTilesHelper = require("helpers.fake_tiles")

local fallingBlock = {}

fallingBlock.name = "SorbetHelper/DashFallingBlock"
fallingBlock.depth = -9000
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
        dashCornerCorrection = false,
        direction = "Down",
        fallDashMode = "Disabled",
        width = 8,
        height = 8
    }
}

fallingBlock.sprite = fakeTilesHelper.getEntitySpriteFunction("tiletype", false)

fallingBlock.fieldInformation = {
    tiletype = {
        options = function()
            return fakeTilesHelper.getTilesOptions()
        end,
        editable = false,
    },
    direction = {
        options = { "Down", "Up", "Left", "Right" },
        editable = false
    },
    fallDashMode = {
        options = { "Disabled", "Push", "Pull" },
        editable = false
    }
}

fallingBlock.fieldOrder = {"x", "y", "width", "height", "shakeSfx", "impactSfx", "tiletype", "depth", "direction", "fallDashMode", "fallOnTouch", "climbFall", "fallOnStaticMover", "allowWavedash", "dashCornerCorrection"}

return fallingBlock
