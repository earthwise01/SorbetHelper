local drawableNinePatch = require("structs.drawable_nine_patch")
local drawableSprite = require("structs.drawable_sprite")
local enums = require("consts.celeste_enums")
local utils = require("utils")

local exclamationBlock = {}

exclamationBlock.name = "SorbetHelper/ExclamationBlock"
exclamationBlock.nodeLimits = {1, -1}
exclamationBlock.nodeLineRenderType = "line"
exclamationBlock.nodeLineRenderOffset = {0, 0}
exclamationBlock.nodeTexture = "objects/SorbetHelper/exclamationBlock/nodeMarker"
exclamationBlock.minimumSize = {16, 16}
exclamationBlock.placements = {
    name = "exclamationBlock",
    data = {
        width = 16,
        height = 16,
        moveSpeed = 160.0,
        activeTime = 5.0,
        canRefreshTimer = false,
        pauseTimerWhileExtending = true,
        canWavedash = false,
        extendTo = "1,2,3",
        spriteDirectory = "objects/SorbetHelper/exclamationBlock",
        drawOutline = true,
    }
}

exclamationBlock.fieldInformation = {
    extendTo = {
        options = { "NextBlock" },
        editable = true
    },
    spriteDirectory = {
        options = { "objects/SorbetHelper/exclamationBlock" },
        editable = true
    },
}

exclamationBlock.fieldOrder = {
    "x", "y", "width", "height", "moveSpeed", "activeTime", "spriteDirectory", "extendTo", "drawOutline", "canWavedash", "pauseTimerWhileExtending", "canRefreshTimer"
}

local ninePatchOptions = {
    mode = "fill",
    borderMode = "repeat",
    fillMode = "repeat"
}

local function collideRectPoint(rectX, rectY, rectW, rectH, pointX, pointY)
    if pointX >= rectX and pointX < rectX + rectW then
        return pointY >= rectY and pointY < rectY + rectH
    end
    return false
end

local function addNinePatchSprite(spriteTable, x, y, width, height, texture, color)
    color = color or {1.0, 1.0, 1.0, 1.0}

    local ninePatch = drawableNinePatch.fromTexture(texture, ninePatchOptions, x, y, width, height)
    ninePatch.color = color
    local sprites = ninePatch:getDrawableSprite()

    for _,v in ipairs(sprites) do
        table.insert(spriteTable, v)
    end
end

local function addSegmentSprites(entity, spriteTable, texture)
    local color = {1.0, 1.0, 1.0, 0.2}
    local nodes = entity.nodes or {}
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 24, entity.height or 24

    for _,node in ipairs(nodes) do
        local nx, ny = node.x or 0, node.y or 0
        local direction
        while not collideRectPoint(x, y, width, height, nx, ny) do
            direction = (x < nx ? 1 : -1)
            if x > nx or x + width <= nx then
                x += width * direction
                addNinePatchSprite(spriteTable, x, y, width, height, texture, color)
            end

            direction = (y < ny ? 1 : -1)
            if y > ny or y + height <= ny then
                y += height * direction
                addNinePatchSprite(spriteTable, x, y, width, height, texture, color)
            end
        end
    end
end

function exclamationBlock.sprite(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 24, entity.height or 24

    local spriteDirectory = entity.spriteDirectory or "objects/SorbetHelper/exclamationBlock"

    local activeTexture = spriteDirectory .. "/activeBlock"
    local emptyTexture = spriteDirectory .. "/emptyBlock"
    local exclamationTexture = spriteDirectory .. "/exclamationMark"

    local sprites = {}
    addNinePatchSprite(sprites, x, y, width, height, activeTexture)
    addSegmentSprites(entity, sprites, emptyTexture)
    local exclamationSprite = drawableSprite.fromTexture(exclamationTexture, entity)
    exclamationSprite:addPosition(math.floor(width / 2), math.floor(height / 2))
    table.insert(sprites, exclamationSprite)

    return sprites
end

function exclamationBlock.nodeSprite(room, entity, node, nodeIndex, viewport)
    local targetNodes = ((string.split(entity.extendTo or "", ",") or ${}) + tostring(#entity.nodes or 0))
    local nodeColor = (targetNodes:contains(tostring(nodeIndex)) ? {0 / 255, 255 / 255, 255 / 255, 153 / 255} : {255 / 255, 0 / 255, 255 / 255, 153 / 255})

    local nx, ny = node.x or 0, node.y or 0
    local sprite = drawableSprite.fromTexture("objects/SorbetHelper/exclamationBlock/nodeMarker", {x = nx, y = ny, color = nodeColor})
    sprite.rotation = math.pi / 4
    sprite:setJustification(0.5, 0.5)
    sprite:setScale(0.9, 0.9)

    return sprite
end

function exclamationBlock.selection(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 24, entity.height or 24
    local nodes = entity.nodes or {}

    local nodeSelection = {}

    for _,node in ipairs(nodes) do
        local nx, ny = node.x or 0, node.y or 0
        table.insert(nodeSelection, utils.rectangle(nx - 3, ny - 3, 6, 6))
    end

    return utils.rectangle(x, y, width, height), nodeSelection
end

return exclamationBlock