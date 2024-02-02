local drawableSprite = require("structs.drawable_sprite")
local drawableLine = require("structs.drawable_line")
local utils = require("utils")
local drawing = require("utils.drawing")
local entities = require("entities")

local return_berry = {}

return_berry.name = "SorbetHelper/ReturnBerry"
return_berry.depth = -100
return_berry.nodeLineRenderType = "fan"
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
            "x", "y", "delay", "winged"
        }
    else
        fields = {
            "x", "y", "checkpointID", "order", "delay", "winged"
        }
    end
    return fields
end

return_berry.placements = {
    {
        name = "normal",
        placementType = "point",
        data = {
            winged = false,
            checkpointID = -1,
            order = -1,
            delay = 0.3,
            nodes = {
                {x = 0, y = 0},
                {x = 0, y = 0}
            }
        }
    },
    {
        name = "winged",
        placementType = "point",
        data = {
            winged = true,
            checkpointID = -1,
            order = -1,
            delay = 0.3,
            nodes = {
                {x = 0, y = 0},
                {x = 0, y = 0}
            }
        }
    }
}

function return_berry.sprite(room, entity, viewport)
    local x, y = entity.x or 0, entity.y or 0
    local winged = entity.winged
    local seeded = entity.nodes and #entity.nodes > 2
    local bubble_sprite = "characters/player/bubble"
    local sprite = ""

    if winged then
        if seeded then
            sprite = "collectables/ghostberry/wings01"
        else
            sprite = "collectables/strawberry/wings01"
        end
    else
        if seeded then
            sprite = "collectables/ghostberry/idle00"
        else
            sprite = "collectables/strawberry/normal00"
        end
    end

    local nodes = entity.nodes

    local mx, my = nodes[1].x, nodes[1].y
    local nx, ny = nodes[2].x, nodes[2].y

    return {
        drawableSprite.fromTexture(sprite, entity),
        drawableSprite.fromTexture(bubble_sprite, {x = mx, y = my, color = {255 / 255, 255 / 255, 255 / 255, 80 / 255}}),
        drawableSprite.fromTexture(bubble_sprite, {x = nx, y = ny, color = {255 / 255, 255 / 255, 255 / 255, 185 / 255}}),
        drawableLine.fromPoints(drawing.getSimpleCurve({x, y}, {nx, ny}, {mx, my}), {255 / 255, 255 / 255, 255 / 255, 195 / 255}),
        drawableLine.fromPoints({x, y, mx, my, nx, ny}, {255 / 255, 255 / 255, 255 / 255, 40 / 255})
    }
end

function return_berry.nodeTexture(room, entity, node, nodeIndex, viewport)
    if nodeIndex < 3 then
        return false
    else
        return "collectables/strawberry/seed00"
    end
end

function return_berry.selection(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local winged = entity.winged
    local mainRectangle

    if winged then
        mainRectangle = utils.rectangle(x - 17, y - 7, 34, 13)
    else
        mainRectangle = utils.rectangle(x - 5, y - 7, 10, 13)
    end

    local nodes = entity.nodes or {{x = 0, y = 0}, {x = 0, y = 0}}
    local nodeRectangles = {}
    for i, node in ipairs(nodes) do
        if i < 3 then
            table.insert(nodeRectangles, utils.rectangle(node.x - 11, node.y - 11, 23, 23))
        else
            table.insert(nodeRectangles, utils.rectangle(node.x - 4, node.y - 4, 7, 10))
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