local fakeTilesHelper = require("helpers.fake_tiles")
local sorbetHelper = require("mods").requireFromPlugin("libraries.sorbet_helper")

local function createFallingBlockPlugin(entityName, placementName, placementAltName, fieldOrder, extraPlacementData, extraFieldInformation)
    local fallingBlock = {}

    local directions = {
        "Down",
        "Up",
        "Left",
        "Right"
    }

    fallingBlock.name = entityName

    local placementData = {
        width = 8,
        height = 8,
        direction = "Down",
        flagOnFall = "",
        flagOnLand = "",
        triggerFlag = "",
        resetFlags = false,
        fallOnTouch = true,
        climbFall = true,
        fallOnStaticMover = true,
        breakDashBlocks = true,
        ignoreSolids = false,
        initialShakeTime = 0.2,
        variableShakeTime = 0.4,
        maxSpeed = 160,
        acceleration = 500,
        shakeSfx = "event:/game/general/fallblock_shake",
        impactSfx = "event:/game/general/fallblock_impact",
        depth = -9000,
        chronoHelperGravityChangeShakeTime = 0.0, -- grrr
    }

    if extraPlacementData then
        for k, v in pairs(extraPlacementData) do
            placementData[k] = v
        end
    end

    function fallingBlock.placements()
        placementData.tiletype = fakeTilesHelper.getPlacementMaterial()

        return {
            name = placementName,
            alternativeName = placementAltName,
            data = placementData
        }
    end

    fallingBlock.ignoredFields = { "_id", "_name", "chronoHelperGravityChangeShakeTime" }

    fallingBlock.fieldOrder = fieldOrder

    fallingBlock.fieldInformation = {
        tiletype = {
            options = function() return fakeTilesHelper.getTilesOptions() end,
            editable = false,
        },
        direction = {
            options = directions,
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
            options = sorbetHelper.getDepths(),
            editable = true
        }
    }

    if extraFieldInformation then
        for k, v in pairs(extraFieldInformation) do
            fallingBlock.fieldInformation[k] = v
        end
    end

    fallingBlock.sprite = fakeTilesHelper.getEntitySpriteFunction("tiletype", false)

    function fallingBlock.depth(room, entity)
        return entity.depth or -9000
    end

    return fallingBlock
end

local customFallingBlockFieldOrder = {
    "x", "y",
    "width", "height",
    "shakeSfx", "impactSfx",
    "triggerFlag", "depth",
    "flagOnFall", "flagOnLand",
    "initialShakeTime", "variableShakeTime",
    "acceleration", "maxSpeed",
    "tiletype", "direction",
    "fallOnTouch", "climbFall", "fallOnStaticMover", "breakDashBlocks",
    "ignoreSolids", "resetFlags", "chronoHelperGravity"
}

local dashFallingBlockPlacementData = {
    allowWavedash = false,
    dashCornerCorrection = true,
    refillDash = false,
    fallDashMode = "Disabled",
    -- dashActivated = true, -- not needed anymore since custom falling blocks exist as their own entity
    fallOnTouch = false,
    fallOnStaticMover = false,
}

local dashFallingBlockFieldOrder = {
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
    "refillDash", "dashActivated", "ignoreSolids", "resetFlags", "chronoHelperGravity"
}

local dashFallingBlockFieldInfo = {
    fallDashMode = {
        options = {
            "Disabled",
            "Push",
            "Pull"
        },
        editable = false
    }
}

local gravityFallingBlockAssociatedMods = {
    "SorbetHelper",
    "ChronoHelper"
}

local customFallingBlock = createFallingBlockPlugin("SorbetHelper/CustomFallingBlock", "custom_falling_block", nil, customFallingBlockFieldOrder, nil, nil)
local dashFallingBlock = createFallingBlockPlugin("SorbetHelper/DashFallingBlock", "dash_falling_block", nil, dashFallingBlockFieldOrder, dashFallingBlockPlacementData, dashFallingBlockFieldInfo)
local customGravityFallingBlock = createFallingBlockPlugin("SorbetHelper/CustomGravityFallingBlock", "custom_gravity_falling_block", "gravity_custom_falling_block", customFallingBlockFieldOrder, nil, nil)
local gravityDashFallingBlock = createFallingBlockPlugin("SorbetHelper/GravityDashFallingBlock", "gravity_dash_falling_block", "dash_gravity_falling_block", dashFallingBlockFieldOrder, dashFallingBlockPlacementData, dashFallingBlockFieldInfo)
customGravityFallingBlock.associatedMods = gravityFallingBlockAssociatedMods
gravityDashFallingBlock.associatedMods = gravityFallingBlockAssociatedMods

return {
    customFallingBlock,
    dashFallingBlock,
    customGravityFallingBlock,
    gravityDashFallingBlock
}
