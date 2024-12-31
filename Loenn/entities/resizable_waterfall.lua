local drawableRectangle = require("structs.drawable_rectangle")
local roomStruct = require("structs.room")
local xnaColors = require("consts.xna_colors")
local utils = require("utils")
local waterfallHelper = require("helpers.waterfalls")
local connectedEntities = require("helpers.connected_entities")

local mods = require("mods")
local depths = mods.requireFromPlugin("libraries.depths")

local resizableWaterfall = {}

resizableWaterfall.name = "SorbetHelper/BigWaterfall"
resizableWaterfall.canResize = {true, false}
resizableWaterfall.minimumSize = {8, 0}

resizableWaterfall.fieldInformation = {
    depth = {
        fieldType = "integer",
        options = depths.addDepths(depths.getDepths(), {
            {"Water & Waterfalls", -9999}, {"FG Waterfalls", -49900}
        }),
        editable = true
    },
    color = {
        fieldType = "color",
        allowXNAColors = false
    },
    wavePercent = {
        options = {1.0, 0.8},
        minimumValue = 0,
        maximumValue = 1
    }
}

resizableWaterfall.fieldOrder = {
    "x", "y", "width", "color", "depth", "ignoreSolids", "lines", "wavePercent"
}

resizableWaterfall.ignoredFields = {
    "height", "_id", "_name"
}

resizableWaterfall.placements = {
    {
        name = "normal",
        alternativeName = "bigwaterfall",
        data = {
            width = 8,
            color = "87cefa",
            ignoreSolids = false,
            lines = true,
            wavePercent = 1.0,
            depth = -9999
        }
    },
    {
        name = "small",
        alternativeName = "bigwaterfall",
        data = {
            width = 8,
            color = "87cefa",
            ignoreSolids = false,
            lines = false,
            wavePercent = 0.8,
            depth = -9999
        }
    },
    {
        name = "abovefg",
        alternativeName = "bigwaterfall",
        data = {
            width = 8,
            color = "87cefa",
            ignoreSolids = true,
            lines = true,
            wavePercent = 1.0,
            depth = -49900
        }
    }
}

-- normally loenn doesnt check for pandoras box water but i do here anyway bc itd look weird sometimes otherwise
local function waterSearchPredicate(entity)
    return entity._name == "water" or entity._name == "pandorasBox/coloredWater"
end

local function anyCollisions(rectangle, rectangles)
    for _, rect in ipairs(rectangles) do
        if utils.aabbCheck(rect, rectangle) then
            return true
        end
    end

    return false
end

-- stolen from loenn waterfallHelper but with the ability to ignore tiles
local function getWaterfallHeight(room, entity, ignoreTiles)
    local waterBlocks = utils.filter(waterSearchPredicate, room.entities)
    local waterRectangles = connectedEntities.getEntityRectangles(waterBlocks)

    local x, y = entity.x or 0, entity.y or 0
    local tileX, tileY = math.floor(x / 8) + 1, math.floor(y / 8) + 1

    local roomHeight = room.height
    local wantedHeight = 8 - y % 8

    local tileMatrix = room.tilesFg.matrix

    while wantedHeight < roomHeight - y do
        local rectangle = utils.rectangle(x, y + wantedHeight, 8, 8)

        if anyCollisions(rectangle, waterRectangles) then
            break
        end

        if not ignoreTiles and tileMatrix:get(tileX, tileY + 1, "0") ~= "0" then
            break
        end

        wantedHeight += 8
        tileY += 1
    end

    return wantedHeight
end

local function getWithAlpha(color, alpha)
    return { color[1] * alpha, color[2] * alpha, color[3] * alpha, alpha }
end

-- adapted from loenn's waterfall helper with some edits
local function getWaterfallSprite(room, entity, fillColor, borderColor)
    local x, y = entity.x or 0, entity.y or 0
    local width, height = entity.width or 16, getWaterfallHeight(room, entity, entity.ignoreSolids)

    local hasLines = entity.lines or false
    -- local linesOffset = (width <= 8 ? 1 : 2)
    local borderOffset = (width <= 8 ? 1 : 2)

    local sprites = {}

    local middleRectangle = drawableRectangle.fromRectangle("fill", x, y, width, height, fillColor)
    local leftRectangle = drawableRectangle.fromRectangle("fill", x - 1, y, borderOffset, height, borderColor)
    local rightRectangle = drawableRectangle.fromRectangle("fill", x + width + 1 - borderOffset, y, borderOffset, height, borderColor)

    table.insert(sprites, middleRectangle:getDrawableSprite())
    table.insert(sprites, leftRectangle:getDrawableSprite())
    table.insert(sprites, rightRectangle:getDrawableSprite())

    local addWaveLineSprite = waterfallHelper.addWaveLineSprite

    -- Add wave pattern
    for i = 0, height, 21 do
        -- From left to right in the waterfall
        -- Parts connected to side borders
        addWaveLineSprite(sprites, y, height, x - 1 + borderOffset, y + i + 9, 1, 12, borderColor)
        addWaveLineSprite(sprites, y, height, x + borderOffset, y + i + 11, 1, 8, borderColor)
        addWaveLineSprite(sprites, y, height, x + width - 1 - borderOffset, y + i, 1, 9, borderColor)
        addWaveLineSprite(sprites, y, height, x + width - 0 - borderOffset, y + i - 2, 1, 13, borderColor)

        if hasLines then
            -- Wave on left border
            addWaveLineSprite(sprites, y, height, x + 0 + borderOffset, y + i, 1, 9, borderColor)
            addWaveLineSprite(sprites, y, height, x + 1 + borderOffset, y + i + 9, 1, 2, borderColor)
            addWaveLineSprite(sprites, y, height, x + 1 + borderOffset, y + i + 19, 1, 2, borderColor)
            addWaveLineSprite(sprites, y, height, x + 2 + borderOffset, y + i + 11, 1, 8, borderColor)

            -- Wave on right border
            addWaveLineSprite(sprites, y, height, x + width - 1 - borderOffset, y + i - 10, 1, 8, borderColor)
            addWaveLineSprite(sprites, y, height, x + width - 2 - borderOffset, y + i - 2, 1, 2, borderColor)
            addWaveLineSprite(sprites, y, height, x + width - 2 - borderOffset, y + i + 9, 1, 2, borderColor)
            addWaveLineSprite(sprites, y, height, x + width - 3 - borderOffset, y + i, 1, 9, borderColor)
        end
    end

    return sprites
end

function resizableWaterfall.sprite(room, entity)
    local color = utils.getColor(entity.color)
    local fillColor = getWithAlpha(color, 0.3)
    local borderColor = getWithAlpha(color, 0.8)

    local result = getWaterfallSprite(room, entity, fillColor, borderColor)

    return result
end

function resizableWaterfall.rectangle(room, entity)
    local x, y = entity.x or 0, entity.y or 0
    local height = getWaterfallHeight(room, entity, entity.ignoreSolids)

    return utils.rectangle(x, y, entity.width, height)
end

function resizableWaterfall.depth(room, entity)
    if entity.ignoreSolids then
        return -49900
    end

    return -9999
end

return resizableWaterfall
