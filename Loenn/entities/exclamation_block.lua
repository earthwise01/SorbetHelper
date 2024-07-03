local drawableNinePatch = require("structs.drawable_nine_patch")
local drawableSprite = require("structs.drawable_sprite")
local enums = require("consts.celeste_enums")
local utils = require("utils")

local exclamationBlock = {}

exclamationBlock.name = "SorbetHelper/ExclamationBlock"
exclamationBlock.placements = {
    name = "exclamationBlock",
    data = {
        width = 24,
        height = 24,
        moveSpeed = 128.0,
        autoExtend = false,
        activeTime = 3.0
    }
}

local ninePatchOptions = {
    mode = "fill",
    borderMode = "repeat",
    fillMode = "repeat"
}

local frameTexture = "objects/SorbetHelper/exclamationBlock/activeBlock"
local middleTexture = "objects/SorbetHelper/exclamationBlock/exclamationMark"

function exclamationBlock.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 24, entity.height or 24

    local blockSprite = entity.sprite or "block"
    local frame = string.format(frameTexture, blockSprite)

    local ninePatch = drawableNinePatch.fromTexture(frame, ninePatchOptions, x, y, width, height)
    local middleSprite = drawableSprite.fromTexture(middleTexture, entity)
    local sprites = ninePatch:getDrawableSprite()

    middleSprite:addPosition(math.floor(width / 2), math.floor(height / 2) + 4)
    table.insert(sprites, middleSprite)

    return sprites
end

return exclamationBlock