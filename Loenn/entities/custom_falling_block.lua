local fakeTilesHelper = require("helpers.fake_tiles")

local function getPlacements(name, extraFields)
    local data = {
        width = 8,
        height = 8,
        direction = "Down",
        flagOnFall = "",
        flagOnLand = "",
        triggerFlag = "",
        fallOnTouch = true,
        climbFall = true,
        fallOnStaticMover = true,
        breakDashBlocks = true,
        initialShakeTime = 0.2,
        variableShakeTime = 0.4,
        maxSpeed = 160,
        acceleration = 500,
        shakeSfx = "event:/game/general/fallblock_shake",
        impactSfx = "event:/game/general/fallblock_impact",
        depth = -9000,
        chronoHelperGravity = false,
    }

    if extraFields then
        for k,v in pairs(extraFields) do

            data[k] = v
        end
    end

    return function()
        data.tiletype = fakeTilesHelper.getPlacementMaterial()

        return {
            name = name,
            data = data
        }
    end
end

local function getFieldInfo(extraFields)
    local fields = {
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
        initialShakeTime = {
            minimumValue = 0.0
        },
        variableShakeTime = {
            minimumValue = 0.0
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
        depth = {
            fieldType = "integer",
            options = depths.getDepths(),
            editable = true
        }
    }

    if extraFields then
        for k,v in pairs(extraFields) do
            fields[k] = v
        end
    end

    return fields
end

local customFallingBlock = {}
local dashFallingBlock = {}

customFallingBlock.name = "SorbetHelper/CustomFallingBlock"
dashFallingBlock.name = "SorbetHelper/DashFallingBlock"
customFallingBlock.depth = -9000
dashFallingBlock.depth = -9000

customFallingBlock.placements = getPlacements("customFallingBlock")
dashFallingBlock.placements = getPlacements("dashFallingBlock", {
    allowWavedash = false,
    dashCornerCorrection = true,
    refillDash = false,
    fallDashMode = "Disabled",
    dashActivated = true,
    fallOnTouch = false,
    fallOnStaticMover = false,
})

customFallingBlock.sprite = fakeTilesHelper.getEntitySpriteFunction("tiletype", false)
dashFallingBlock.sprite = fakeTilesHelper.getEntitySpriteFunction("tiletype", false)

customFallingBlock.fieldInformation = getFieldInfo()
dashFallingBlock.fieldInformation = getFieldInfo({
    fallDashMode = {
        options = { "Disabled", "Push", "Pull" },
        editable = false
    }
})

-- this is   manual because i hate myself  !
customFallingBlock.fieldOrder = {
    "x", "y",
    "width", "height",
    "shakeSfx", "impactSfx",
    "triggerFlag", "depth",
    "flagOnFall", "flagOnLand",
    "initialShakeTime", "variableShakeTime",
    "acceleration", "maxSpeed",
    "tiletype", "direction", 
    "fallOnTouch", "climbFall", "fallOnStaticMover", "breakDashBlocks",
    "chronoHelperGravity"
}

dashFallingBlock.fieldOrder = {
    "x", "y",
    "width", "height",
    "shakeSfx", "impactSfx",
    "triggerFlag", "depth",
    "flagOnFall", "flagOnLand",
    "initialShakeTime", "variableShakeTime",
    "acceleration", "maxSpeed",
    "direction", "fallDashMode",
    "tiletype", "fallOnTouch", "climbFall",
    "allowWavedash", "dashCornerCorrection", "fallOnStaticMover", "breakDashBlocks",
    "refillDash", "dashActivated", "chronoHelperGravity"
}

return { customFallingBlock, dashFallingBlock }
