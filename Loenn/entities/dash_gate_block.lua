local drawableNinePatch = require("structs.drawable_nine_patch")
local drawableSprite = require("structs.drawable_sprite")

-- Some of this plugin is copy-pasted from MaxHelpingHand's Flag Switch Gates
-- https://github.com/max4805/MaxHelpingHand/blob/master/Loenn/entities/flagSwitchGate.lua

local dashGateBlock = {}

dashGateBlock.name = "SorbetHelper/DashGateBlock"
dashGateBlock.depth = 0
dashGateBlock.nodeLimits = {1, 1}
dashGateBlock.nodeLineRenderType = "line"
dashGateBlock.minimumSize = {16, 16}
dashGateBlock.placements = {}

local textures = {
    "block", "mirror", "temple", "stars"
}

for i, texture in ipairs(textures) do
    dashGateBlock.placements[i] = {
        name = texture,
        data = {
            width = 16,
            height = 16,
            blockSprite = texture,
            iconSprite = "SorbetHelper/gateblock/dash/icon",
            inactiveColor = "F86593",
            activeColor = "FFFFFF",
            finishColor = "62A1F5",
            shakeTime = 0.5,
            moveTime = 1.8,
            moveEased = true,
            moveSound = "event:/game/general/touchswitch_gate_open",
            finishedSound = "event:/game/general/touchswitch_gate_finish",
            allowWavedash = false,
            dashCornerCorrection = false,
            persistent = false,
            smoke = true,
            linked = false,
            linkTag = ""
        }
    }
end

dashGateBlock.fieldOrder = {"x", "y", "width", "height", "inactiveColor", "activeColor", "finishColor", "moveSound", "finishedSound", "shakeTime", "moveTime", "moveEased", "blockSprite", "iconSprite", "allowWavedash", "dashCornerCorrection", "smoke", "persistent", "linked", "linkTag"}

dashGateBlock.fieldInformation = {
    inactiveColor = {
        fieldType = "color"
    },
    activeColor = {
        fieldType = "color"
    },
    finishColor = {
        fieldType = "color"
    }
}

local ninePatchOptions = {
    mode = "fill",
    borderMode = "repeat",
    fillMode = "repeat"
}

local frameTexture = "objects/switchgate/%s"
local middleTexture = "objects/SorbetHelper/gateblock/dash/icon00"

function dashGateBlock.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 24, entity.height or 24

    local blockSprite = entity.blockSprite or "block"
    local frame = string.format(frameTexture, blockSprite)

    local ninePatch = drawableNinePatch.fromTexture(frame, ninePatchOptions, x, y, width, height)
    local middleSprite = drawableSprite.fromTexture(middleTexture, entity)
    local sprites = ninePatch:getDrawableSprite()

    middleSprite:addPosition(math.floor(width / 2), math.floor(height / 2))
    table.insert(sprites, middleSprite)

    return sprites
end

return dashGateBlock
