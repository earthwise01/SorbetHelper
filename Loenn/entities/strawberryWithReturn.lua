local drawableSprite = require("structs.drawable_sprite")
local drawableLine = require("structs.drawable_line")
local utils = require("utils")
local drawing = require("utils.drawing")
local colors = require("consts.colors")
local entities = require("entities")
local sorbetUtils = require("mods").requireFromPlugin("libraries.utils")

local return_berry = {}

return_berry.name = "SorbetHelper/ReturnBerry"
return_berry.depth = -100
return_berry.nodeLineRenderType = false -- done manually to seperate seeds and return position
return_berry.nodeLimits = {2, -1}
return_berry.ignoredFields = { "nodes", "_name", "_id", "originX", "originY" }

return_berry.fieldInformation = {
    order = {
        fieldType = "integer",
    },
    checkpointID = {
        fieldType = "integer"
    },
    delay = {
        minimumValue = 0.0
    }
}

function return_berry.fieldOrder(entity)
    local fields = {}
    if entity.checkpointID == nil then
        fields = {
            "x", "y", "delay", "winged", "moon", "bubbleParticles"
        }
    else
        fields = {
            "x", "y", "checkpointID", "order", "delay", "winged", "moon", "bubbleParticles"
        }
    end
    return fields
end

return_berry.placements = {
    {
        name = "normal",
        alternativeName = "normalalt",
        placementType = "point",
        data = {
            winged = false,
            moon = false,
            checkpointID = -1,
            order = -1,
            delay = 0.3,
            bubbleParticles = true,
            nodes = {
                {x = 0, y = 0},
                {x = 0, y = 0}
            }
        }
    },
    {
        name = "winged",
        alternativeName = "wingedalt",
        placementType = "point",
        data = {
            winged = true,
            moon = false,
            checkpointID = -1,
            order = -1,
            delay = 0.3,
            bubbleParticles = true,
            nodes = {
                {x = 0, y = 0},
                {x = 0, y = 0}
            }
        }
    }
}

local function getWhite(opacity)
    return {1, 1, 1, opacity or 1}
end

local function addSeeds(sprites, entity, lines)
    local seedTexture = "collectables/strawberry/seed00"
    local lineColor = colors.selectionCompleteNodeLineColor

    local nodes = entity.nodes
    local entityX, entityY = entity.x or 0, entity.y or 0

    -- skip if not seeded
    if not nodes or #nodes < 3 then
        return
    end

    for i,node in ipairs(nodes) do
        if i > 2 then
            local nodeX, nodeY = node.x or 0, node.y or 0

            if lines then
                local line = drawableLine.fromPoints({entityX, entityY, nodeX, nodeY}, lineColor)
                table.insert(sprites, line)
            else
                local seedSprite = drawableSprite.fromTexture(seedTexture, {x = nodeX, y = nodeY})
                table.insert(sprites, seedSprite)
            end
        end
    end
end

function return_berry.sprite(room, entity, viewport)
    local bubbleTexture = "characters/player/bubble"
    local smallBubbleTexture = "particles/bubble"
    local berryTexture = ""

    local x, y = entity.x or 0, entity.y or 0
    local winged = entity.winged
    local moon = entity.moon
    local nodes = entity.nodes
    local seeded = nodes and #nodes > 2

    if moon then
        if winged or seeded then
            berryTexture = "collectables/moonBerry/ghost00"
        else
            berryTexture = "collectables/moonBerry/normal00"
        end

    else
        if winged then
            if seeded then
                berryTexture = "collectables/ghostberry/wings01"
            else
                berryTexture = "collectables/strawberry/wings01"
            end

        else
            if seeded then
                berryTexture = "collectables/ghostberry/idle00"
            else
                berryTexture = "collectables/strawberry/normal00"
            end
        end
    end

    local sprites = {}

    -- bubbles behind berry
    table.insert(sprites, drawableSprite.fromTexture(smallBubbleTexture, {x = x + 2, y = y - 8, color = getWhite(0.25)}))

    -- berry
    table.insert(sprites, drawableSprite.fromTexture(berryTexture, entity))

    -- bubbles above berry
    table.insert(sprites, drawableSprite.fromTexture(smallBubbleTexture, {x = x - 5, y = y - 2, color = getWhite(0.6)}))
    table.insert(sprites, drawableSprite.fromTexture(smallBubbleTexture, {x = x + 4, y = y + 3, color = getWhite(0.6)}))

    -- end bubble
    local nx, ny = nodes[2].x or 0, nodes[2].y or 0
    table.insert(sprites, drawableSprite.fromTexture(bubbleTexture, {x = nx, y = ny, color = getWhite(0.5)}))

    -- seeds
    addSeeds(sprites, entity, false)

    return sprites
end

function return_berry.nodeSprite(room, entity, node, nodeIndex, viewport)
    -- node sprite is used for stuff that should only be rendered when selected so im wrapping it all in the "first node"
    if nodeIndex > 1 then
        return {}
    end

    local bubbleTexture = "characters/player/bubble"

    local nodes = entity.nodes

    local x, y = entity.x or 0, entity.y or 0
    local endX, endY = nodes[2].x or 0, nodes[2].y or 0
    local anchorX, anchorY = nodes[1].x or 0, nodes[1].y or 0

    local sprites = {}

    -- return curve
    table.insert(sprites, drawableLine.fromPoints(drawing.getSimpleCurve({x, y}, {endX, endY}, {anchorX, anchorY}), getWhite(0.65)))
    table.insert(sprites, drawableLine.fromPoints({x, y, anchorX, anchorY, endX, endY}, getWhite(0.2)))
    table.insert(sprites, sorbetUtils.getGenericNodeSprite(anchorX, anchorY, getWhite(0.8)))

    -- seed lines
    addSeeds(sprites, entity, true)

    return sprites-- drawableSprite.fromTexture("util/noise", node)
end

function return_berry.selection(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local winged = entity.winged
    local moon = entity.moon
    local mainRectangle

    if moon then
        mainRectangle = utils.rectangle(x - 8, y - 7, 16, 13)
    elseif winged then
        mainRectangle = utils.rectangle(x - 17, y - 7, 34, 13)
    else
        mainRectangle = utils.rectangle(x - 5, y - 7, 10, 13)
    end

    local nodes = entity.nodes or {{x = 0, y = 0}, {x = 0, y = 0}}
    local nodeRectangles = {}
    for i, node in ipairs(nodes) do
        local nx, ny = node.x or 0, node.y or 0

        if i == 1 then
            table.insert(nodeRectangles, utils.rectangle(nx - 4, ny - 4, 8, 8))
        elseif i == 2 then
            table.insert(nodeRectangles, utils.rectangle(nx - 11, ny - 11, 23, 23))
        else
            table.insert(nodeRectangles, utils.rectangle(nx - 4, ny - 4, 7, 10))
        end
    end

    return mainRectangle, nodeRectangles
end

function return_berry.nodeAdded(room, entity, nodeIndex)
    local nodes = entity.nodes or {{x = 0, y = 0}, {x = 0, y = 0}}
    if nodeIndex == 0 then
        local nodeX = entity.x + (entity.width or 0) + 8
        local nodeY = entity.y

        table.insert(nodes, 3, {x = nodeX, y = nodeY})
    else
        local nodeX = nodes[nodeIndex].x + (entity.width or 0) + 8
        local nodeY = nodes[nodeIndex].y

        if nodeIndex == 1 then
            table.insert(nodes, 3, {x = nodeX, y = nodeY})
        else
            table.insert(nodes, nodeIndex + 1, {x = nodeX, y = nodeY})
        end
    end

    return true
end

function return_berry.delete(room, targetEntity, nodeIndex)
    local entities = entities.getRoomItems(room, "entities")
    local nodes = targetEntity.nodes or {{x = 0, y = 0}, {x = 0, y = 0}}
    for i, entity in ipairs(entities) do
        if entity == targetEntity then
            if nodeIndex < 3 then
                table.remove(entities, i)
            else
                table.remove(nodes, nodeIndex)
            end

            return true
        end
    end
end

return return_berry