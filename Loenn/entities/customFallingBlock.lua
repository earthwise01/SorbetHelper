local fakeTilesHelper = require("helpers.fake_tiles")
local mods = require("mods")
local depths = mods.requireFromPlugin("libraries.depths")

local function getFallingBlock(id, altName, placementData, fieldInfo, fieldOrder)
    -- placements
    local data = {
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
        -- chronoHelperGravity = false,
        chronoHelperGravityChangeShakeTime = 0.0, -- grrr
    }

    if placementData then
        for k,v in pairs(placementData) do

            data[k] = v
        end
    end

    local placements = function()
        data.tiletype = fakeTilesHelper.getPlacementMaterial()

        return {
            name = "fallingBlock",
            alternativeName = altName,
            data = data
        }
    end

    -- field information
    local fieldInformation = {
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

    if fieldInfo then
        for k,v in pairs(fieldInfo) do
            fieldInformation[k] = v
        end
    end

    -- create the falling block
    local fallingBlock = {}
    fallingBlock.name = id
    fallingBlock.depth = function (entity) return entity.depth end
    fallingBlock.placements = placements
    fallingBlock.fieldInformation = fieldInformation
    fallingBlock.ignoredFields = { "_id", "_name", "chronoHelperGravityChangeShakeTime" }
    fallingBlock.fieldOrder = fieldOrder
    fallingBlock.sprite = fakeTilesHelper.getEntitySpriteFunction("tiletype", false)
    return fallingBlock
end

-- dash falling block info
local dashFallingBlockPlacementInfo = {
    allowWavedash = false,
    dashCornerCorrection = true,
    refillDash = false,
    fallDashMode = "Disabled",
    -- dashActivated = true, -- not needed anymore since custom falling blocks exist as their own entity
    fallOnTouch = false,
    fallOnStaticMover = false,
}
local dashFallingBlockFieldInfo = {
    fallDashMode = {
        options = { "Disabled", "Push", "Pull" },
        editable = false
    }
}

-- field orders
-- this is   manual because i hate myself  !
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

local customFallingBlock = getFallingBlock("SorbetHelper/CustomFallingBlock", nil, nil, nil, customFallingBlockFieldOrder)
local dashFallingBlock = getFallingBlock("SorbetHelper/DashFallingBlock", nil, dashFallingBlockPlacementInfo, dashFallingBlockFieldInfo, dashFallingBlockFieldOrder)
local customGravityFallingBlock = getFallingBlock("SorbetHelper/CustomGravityFallingBlock", "altName", nil, nil, customFallingBlockFieldOrder)
local gravityDashFallingBlock = getFallingBlock("SorbetHelper/GravityDashFallingBlock", "altName", dashFallingBlockPlacementInfo, dashFallingBlockFieldInfo, dashFallingBlockFieldOrder)
customGravityFallingBlock.associatedMods = { "SorbetHelper", "ChronoHelper" }
gravityDashFallingBlock.associatedMods = { "SorbetHelper", "ChronoHelper" }

return { customFallingBlock, dashFallingBlock, customGravityFallingBlock, gravityDashFallingBlock }
