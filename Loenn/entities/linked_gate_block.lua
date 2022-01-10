local drawableNinePatch = require("structs.drawable_nine_patch")
local drawableSprite = require("structs.drawable_sprite")

-- Some of this plugin is copy-pasted from MaxHelpingHand's Flag Switch Gates
-- https://github.com/max4805/MaxHelpingHand/blob/master/Loenn/entities/flagSwitchGate.lua

local linkedGateBlock = {}

linkedGateBlock.name = "SorbetHelper/LinkedGateBlock"
linkedGateBlock.depth = 0
linkedGateBlock.nodeLimits = {1, 1}
linkedGateBlock.nodeLineRenderType = "line"
linkedGateBlock.minimumSize = {16, 16}
linkedGateBlock.placements = {}

local textures = {
    "block", "mirror", "temple", "stars"
}

for i, texture in ipairs(textures) do
    linkedGateBlock.placements[i] = {
        name = texture,
        data = {
            width = 16,
            height = 16,
            blockSprite = texture,
            iconSprite = "SorbetHelper/gateblock/linked/icon",
            inactiveColor = "FB8A40",
            activeColor = "FFFFFF",
            finishColor = "34D470",
            shakeTime = 0.5,
            moveTime = 1.8,
            moveEased = true,
            moveSound = "event:/game/general/touchswitch_gate_open",
            finishedSound = "event:/game/general/touchswitch_gate_finish",
            persistent = false,
            smoke = true,
            linkTag = ""
        }
    }
end

linkedGateBlock.fieldOrder = {"x", "y", "width", "height", "inactiveColor", "activeColor", "finishColor", "moveSound", "finishedSound", "shakeTime", "moveTime", "moveEased", "blockSprite", "iconSprite", "smoke", "persistent", "linkTag"}

linkedGateBlock.fieldInformation = {
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
local middleTexture = "objects/SorbetHelper/gateblock/linked/icon00"

function linkedGateBlock.sprite(room, entity)
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

return linkedGateBlock
