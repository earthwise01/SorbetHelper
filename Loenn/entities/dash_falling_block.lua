local fakeTilesHelper = require("helpers.fake_tiles")

local fallingBlock = {}

fallingBlock.name = "SorbetHelper/DashFallingBlock"
fallingBlock.depth = -9000
function fallingBlock.placements()
    return {
        name = "falling_block",
        data = {
            tiletype = fakeTilesHelper.getPlacementMaterial(),
            depth = -9000,
            shakeSfx = "event:/game/general/fallblock_shake",
            impactSfx = "event:/game/general/fallblock_impact",
            fallOnTouch = false,
            climbFall = true,
            fallOnStaticMover = false,
            allowWavedash = false,
            dashCornerCorrection = true,
            breakDashBlocks = true,
            direction = "Down",
            fallDashMode = "Disabled",
            dashActivated = true,
            width = 8,
            height = 8
        }
    }
end

fallingBlock.sprite = fakeTilesHelper.getEntitySpriteFunction("tiletype", false)

fallingBlock.fieldInformation = {
    tiletype = {
        options = function()
            return fakeTilesHelper.getTilesOptions()
        end,
        editable = false,
    },
    shakeSfx = {
        options = {
            "event:/game/general/fallblock_shake",
            "event:/game/01_forsaken_city/fallblock_ice_shake",
            "event:/game/03_resort/fallblock_wood_shake",
            "event:/game/06_reflection/fallblock_boss_shake"
        },
        editable = true
    },
    impactSfx = {
        options = {
            "event:/game/general/fallblock_impact",
            "event:/game/01_forsaken_city/fallblock_ice_impact",
            "event:/game/03_resort/fallblock_wood_impact",
            "event:/game/06_reflection/fallblock_boss_impact"
        },
        editable = true
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

fallingBlock.fieldOrder = {"x", "y", "width", "height", "shakeSfx", "impactSfx", "tiletype", "depth", "direction", "fallDashMode", "fallOnTouch", "climbFall", "allowWavedash", "dashCornerCorrection", "fallOnStaticMover", "dashActivated", "breakDashBlocks"}

return fallingBlock
